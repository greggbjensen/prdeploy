using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Webhooks.Managers;

/// <summary>Acts as a GitHub custom deployment protection rule that only lets a deploy job run when prdeploy's environment lock allows it.</summary>
/// <remarks>
/// When the GitHub App is enabled as a protection rule on an environment, GitHub pauses each job that targets the
/// environment and sends <c>deployment_protection_rule.requested</c>. The deploy workflow is approved only when one
/// pull request holds the environment's lock label and the run deploys that pull request's branch, so a manually
/// dispatched deploy cannot overwrite an environment another pull request is using. Other workflows are approved,
/// because prdeploy only manages the deploy workflow.
/// </remarks>
public partial class DeploymentProtectionManager
{
    private const string LockSuffix = "-lock";

    private readonly WebhookContext _context;
    private readonly PullRequestService _pullRequestService;
    private readonly ILogger<DeploymentProtectionManager> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="context">Webhook context holding the installation client, repository and settings.</param>
    /// <param name="pullRequestService">Reads pull requests, their lock labels and deploy run names.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public DeploymentProtectionManager(WebhookContext context, PullRequestService pullRequestService,
        ILogger<DeploymentProtectionManager> logger)
    {
        _context = context;
        _pullRequestService = pullRequestService;
        _logger = logger;
    }

    /// <summary>Decides whether a paused deploy job may run and reports the decision to GitHub.</summary>
    /// <param name="environment">Environment the job targets.</param>
    /// <param name="callbackUrl">Deployment callback URL from the payload, which holds the workflow run ID.</param>
    public async Task ProcessAsync(string? environment, string? callbackUrl)
    {
        var match = CallbackRunRegex().Match(callbackUrl ?? string.Empty);
        if (string.IsNullOrEmpty(environment) || !match.Success)
        {
            _logger.LogWarning("Unable to read the environment or run from deployment callback {CallbackUrl}.",
                callbackUrl);
            return;
        }

        var runId = long.Parse(match.Groups["runId"].Value);
        var run = await _context.GitHub.Actions.Workflows.Runs.Get(_context.Owner, _context.Repo, runId);
        var decision = await DecideAsync(run, environment);

        _logger.LogInformation("{Decision} deploy run {RunId} to {Environment}: {Comment}",
            decision.Approved ? "Approving" : "Rejecting", runId, environment, decision.Comment);

        // Octokit.NET does not wrap this endpoint.
        // SourceRef: https://docs.github.com/en/rest/actions/workflow-runs#review-custom-deployment-protection-rules-for-a-workflow-run
        await _context.GitHub.Connection.Post(
            new Uri($"repos/{_context.Owner}/{_context.Repo}/actions/runs/{runId}/deployment_protection_rule",
                UriKind.Relative),
            new
            {
                environment_name = environment,
                state = decision.Approved ? "approved" : "rejected",
                comment = decision.Comment
            },
            "application/vnd.github+json",
            CancellationToken.None);
    }

    /// <summary>Decides whether a workflow run may deploy to an environment.</summary>
    /// <param name="run">Workflow run whose job is paused.</param>
    /// <param name="environment">Environment the job targets.</param>
    /// <returns>Whether to approve, with the comment shown in the run.</returns>
    internal async Task<ProtectionDecision> DecideAsync(WorkflowRun run, string environment)
    {
        var deployWorkflow = CheckService.ToWorkflowFileName(_context.Settings.DeployWorkflow);
        if (string.IsNullOrEmpty(deployWorkflow) || CheckService.ToWorkflowFileName(run.Path) != deployWorkflow)
        {
            return new(true, "Not the prdeploy deploy workflow, so prdeploy does not gate it.");
        }

        var lowerEnvironment = environment.ToLowerInvariant();
        var (runEnvironment, branch) = PullRequestService.ParseDeployRunName(run.Name);
        if (runEnvironment != lowerEnvironment)
        {
            return new(false,
                $"This run was not started by prdeploy for {environment}. Comment /deploy {environment} on a pull request instead.");
        }

        var lockLabel = $"{lowerEnvironment}{LockSuffix}";
        var holders = (await _pullRequestService.ListDeployedAsync())
            .Where(p => p.Labels.Any(l => string.Equals(l.Name, lockLabel, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (holders.Count == 0)
        {
            return new(false,
                $"No pull request holds the {environment} lock. Comment /deploy {environment} on a pull request to deploy.");
        }

        if (holders.Count > 1)
        {
            return new(false,
                $"{environment} is locked by more than one pull request ({string.Join(", ", holders.Select(h => $"#{h.Number}"))}). Free it with /free {environment}.");
        }

        var holder = holders[0];
        if (_pullRequestService.IsRollback(run))
        {
            return new(true, $"prdeploy is rolling back {environment}; #{holder.Number} holds the lock.");
        }

        return string.Equals(holder.Head?.Ref, branch, StringComparison.Ordinal)
            ? new(true, $"#{holder.Number} holds the {environment} lock.")
            : new(false, $"{environment} is locked by #{holder.Number}, not this branch.");
    }

    /// <summary>Matches the workflow run ID in a deployment callback URL.</summary>
    /// <returns>The compiled expression.</returns>
    [GeneratedRegex(@"/actions/runs/(?<runId>\d+)/deployment_protection_rule$")]
    private static partial Regex CallbackRunRegex();
}

/// <summary>The answer prdeploy gives a deployment protection rule request.</summary>
/// <param name="Approved">Whether the job may run.</param>
/// <param name="Comment">Explanation shown on the workflow run.</param>
public sealed record ProtectionDecision(bool Approved, string Comment);

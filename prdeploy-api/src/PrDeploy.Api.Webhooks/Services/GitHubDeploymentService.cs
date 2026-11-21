using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Webhooks.Status;
using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>Records prdeploy deploys as GitHub Deployments, so each pull request shows native deployment cards and the repository's Environments page tracks what is active.</summary>
/// <remarks>
/// A deployment is created for the pull request's head commit when the deploy workflow is dispatched, and its status
/// follows the workflow run: queued, in progress, then success or failure. GitHub marks the previous deployment of an
/// environment inactive when a new one succeeds. Every call is best effort: a missing <c>Deployments</c> permission or
/// any other GitHub error is logged and never stops the deploy.
/// </remarks>
public class GitHubDeploymentService
{
    /// <summary>Task name that marks deployments created by prdeploy, so they are not confused with ones GitHub Actions creates for workflow jobs.</summary>
    public const string DeployTask = "deploy:prdeploy";

    /// <summary>Payload key holding the pull request number the deployment belongs to.</summary>
    public const string PullNumberKey = "pull_number";

    private const int DescriptionMaxLength = 140;
    private const int LookupPageSize = 100;

    private readonly WebhookContext _context;
    private readonly EnvironmentUtil _environmentUtil;
    private readonly ILogger<GitHubDeploymentService> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="context">Webhook context holding the installation client and repository.</param>
    /// <param name="environmentUtil">Looks up environment settings such as the URLs shown on the deployment.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public GitHubDeploymentService(WebhookContext context, EnvironmentUtil environmentUtil,
        ILogger<GitHubDeploymentService> logger)
    {
        _context = context;
        _environmentUtil = environmentUtil;
        _logger = logger;
    }

    /// <summary>Creates a queued deployment for a pull request when its deploy workflow has been dispatched.</summary>
    /// <param name="pullRequest">Pull request the deploy was requested on; its head commit is the deployment ref.</param>
    /// <param name="environment">Environment being deployed to.</param>
    /// <param name="deployName">Name of the dispatched deploy run, used as the description.</param>
    /// <param name="workflowFileName">Deploy workflow file name, linked as the log until the run starts.</param>
    /// <param name="rollback">Whether the deploy is a rollback.</param>
    /// <returns>The created deployment ID, or null when it could not be created.</returns>
    public async Task<long?> CreateQueuedAsync(PullRequest pullRequest, string environment, string deployName,
        string workflowFileName, bool rollback)
    {
        var environmentName = GetEnvironmentName(environment);
        try
        {
            // auto_merge would merge the default branch into the ref and required_contexts would wait on commit
            // statuses, both of which make GitHub refuse a deployment for a pull request commit.
            var newDeployment = new NewDeployment(pullRequest.Head.Sha)
            {
                Environment = environmentName,
                Task = DeployTask,
                Description = Truncate(rollback ? $"Rollback: {deployName}" : deployName),
                AutoMerge = false,
                RequiredContexts = new Collection<string>(),
                ProductionEnvironment = string.Equals(environmentName, _context.Settings.ReleaseEnvironment,
                    StringComparison.OrdinalIgnoreCase),
                TransientEnvironment = false,
                Payload = new Dictionary<string, string>
                {
                    [PullNumberKey] = pullRequest.Number.ToString(),
                    ["rollback"] = rollback.ToString().ToLowerInvariant()
                }
            };

            var deployment = await _context.GitHub.Repository.Deployment.Create(_context.Owner, _context.Repo,
                newDeployment);
            if (deployment == null)
            {
                return null;
            }

            await _context.GitHub.Repository.Deployment.Status.Create(_context.Owner, _context.Repo, deployment.Id,
                new NewDeploymentStatus(DeploymentState.Queued)
                {
                    Environment = environmentName,
                    LogUrl = $"{_context.RepositoryHtmlUrl}/actions/workflows/{workflowFileName}",
                    Description = "Deploy workflow dispatched."
                });

            _logger.LogInformation("Created deployment {DeploymentId} for pull request {PullNumber} in {Environment}.",
                deployment.Id, pullRequest.Number, environmentName);
            return deployment.Id;
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex,
                "Unable to create a GitHub deployment for pull request {PullNumber} in {Environment} ({StatusCode}); check the GitHub App has Deployments read and write.",
                pullRequest.Number, environmentName, ex.StatusCode);
            return null;
        }
    }

    /// <summary>Sets the status of a pull request's latest prdeploy deployment from its deploy workflow run.</summary>
    /// <param name="pullNumber">Pull request the deploy belongs to.</param>
    /// <param name="environment">Environment the run deploys to.</param>
    /// <param name="state">Where the run stands.</param>
    /// <param name="run">Deploy workflow run, linked as the deployment log.</param>
    public async Task UpdateFromRunAsync(int pullNumber, string environment, DeployWorkflowState state,
        WorkflowRun run)
    {
        var environmentName = GetEnvironmentName(environment);
        try
        {
            var deployment = await FindLatestAsync(pullNumber, environmentName);
            if (deployment == null)
            {
                _logger.LogDebug("No prdeploy deployment found for pull request {PullNumber} in {Environment}.",
                    pullNumber, environmentName);
                return;
            }

            var (deploymentState, description) = state switch
            {
                DeployWorkflowState.InProgress => (DeploymentState.InProgress, "Deploy workflow running."),
                DeployWorkflowState.Successful => (DeploymentState.Success, "Deploy succeeded."),
                _ => (DeploymentState.Failure, "Deploy failed.")
            };

            var status = new NewDeploymentStatus(deploymentState)
            {
                Environment = environmentName,
                LogUrl = !string.IsNullOrEmpty(run.HtmlUrl)
                    ? run.HtmlUrl
                    : $"{_context.RepositoryHtmlUrl}/actions/runs/{run.Id}",
                Description = description
            };

            if (state == DeployWorkflowState.Successful)
            {
                status.EnvironmentUrl = GetEnvironmentUrl(_environmentUtil.GetSettings(environmentName));
                status.AutoInactive = true;
            }

            await _context.GitHub.Repository.Deployment.Status.Create(_context.Owner, _context.Repo, deployment.Id,
                status);
            _logger.LogInformation("Deployment {DeploymentId} for pull request {PullNumber} is now {State}.",
                deployment.Id, pullNumber, deploymentState);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex,
                "Unable to update the GitHub deployment for pull request {PullNumber} in {Environment} ({StatusCode}).",
                pullNumber, environmentName, ex.StatusCode);
        }
    }

    /// <summary>Finds the newest deployment prdeploy created for a pull request in an environment.</summary>
    /// <param name="pullNumber">Pull request number stored in the deployment payload.</param>
    /// <param name="environmentName">Environment name of the deployment.</param>
    /// <returns>The deployment, or null when none of the repository's recent deployments match.</returns>
    private async Task<Deployment?> FindLatestAsync(int pullNumber, string environmentName)
    {
        // Octokit does not expose the environment filter, so the newest page is filtered here.
        var deployments = await _context.GitHub.Repository.Deployment.GetAll(_context.Owner, _context.Repo,
            new ApiOptions { PageSize = LookupPageSize, PageCount = 1, StartPage = 1 });

        var pullNumberText = pullNumber.ToString();
        return (deployments ?? [])
            .Where(d => d.Task == DeployTask
                        && string.Equals(d.Environment, environmentName, StringComparison.OrdinalIgnoreCase)
                        && d.Payload != null
                        && d.Payload.TryGetValue(PullNumberKey, out var value)
                        && Equals(value?.ToString(), pullNumberText))
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefault();
    }

    /// <summary>Gets the configured name of an environment, so deployments use the same casing as its settings.</summary>
    /// <param name="environment">Environment name in any casing.</param>
    /// <returns>The configured name, or the given name when the environment is not configured.</returns>
    private string GetEnvironmentName(string environment) =>
        _environmentUtil.GetSettings(environment)?.Name ?? environment;

    /// <summary>Gets the URL shown by the deployment's View deployment button.</summary>
    /// <param name="settings">Environment settings, or null when the environment is not configured.</param>
    /// <returns>The first configured http or https URL, or null when there is none.</returns>
    internal static string? GetEnvironmentUrl(EnvironmentSettings? settings)
    {
        var url = settings?.Urls?.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u.Url))?.Url ?? settings?.Url;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? url
            : null;
    }

    /// <summary>Shortens text to the length GitHub accepts for a deployment description.</summary>
    /// <param name="text">Text to shorten.</param>
    /// <returns>The text, cut with an ellipsis when it is too long.</returns>
    private static string Truncate(string text) =>
        text.Length <= DescriptionMaxLength ? text : $"{text[..(DescriptionMaxLength - 1)]}…";
}

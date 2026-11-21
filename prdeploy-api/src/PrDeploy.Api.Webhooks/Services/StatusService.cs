using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Webhooks.Models;
using PrDeploy.Api.Webhooks.Status;
using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>
/// Posts deployment status to pull requests and Slack as deploy workflows start and complete.
/// </summary>
public class StatusService
{
    private const int BuildNameMaxLength = 25;

    // Wait until reposting a release message for the same PR.
    private static readonly TimeSpan PostReleaseMessageDelay = TimeSpan.FromHours(4);
    private static readonly string[] ReportedConclusions = ["success", "failure"];

    private readonly WebhookContext _context;
    private readonly TemplateService _templateService;
    private readonly SlackService _slackService;
    private readonly CheckService _checkService;
    private readonly DeployStateService _deployStateService;
    private readonly PullRequestService _pullRequestService;
    private readonly EnvironmentUtil _environmentUtil;
    private readonly IMemoryCache _cache;
    private readonly DeployStatusBroadcaster _broadcaster;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<StatusService> _logger;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <param name="templateService">Object containing the template service data used by the operation.</param>
    /// <param name="slackService">Object containing the Slack service data used by the operation.</param>
    /// <param name="checkService">Object containing the check service data used by the operation.</param>
    /// <param name="deployStateService">Object containing the deploy state service data used by the operation.</param>
    /// <param name="pullRequestService">Object containing the pull request service data used by the operation.</param>
    /// <param name="environmentUtil">Object containing the environment util data used by the operation.</param>
    /// <param name="cache">Object containing the cache data used by the operation.</param>
    /// <param name="broadcaster">Publishes deploy workflow status to the dashboard stream.</param>
    /// <param name="timeProvider">Supplies the time stamped on published statuses.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public StatusService(WebhookContext context, TemplateService templateService, SlackService slackService,
        CheckService checkService, DeployStateService deployStateService, PullRequestService pullRequestService,
        EnvironmentUtil environmentUtil, IMemoryCache cache, DeployStatusBroadcaster broadcaster,
        TimeProvider timeProvider, ILogger<StatusService> logger)
    {
        _context = context;
        _templateService = templateService;
        _slackService = slackService;
        _checkService = checkService;
        _deployStateService = deployStateService;
        _pullRequestService = pullRequestService;
        _environmentUtil = environmentUtil;
        _cache = cache;
        _broadcaster = broadcaster;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Posts the deploy completed to the configured destination.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="run">Object containing the run data used by the operation.</param>
    public async Task PostDeployCompletedAsync(PullRequest pullRequest, WorkflowRun run)
    {
        var environment = _pullRequestService.GetEnvironment(pullRequest, run);
        _logger.LogInformation("Posting deployment complete message for {Environment} and pull {PullNumber}.",
            environment, pullRequest.Number);

        var conclusion = run.Conclusion?.StringValue;
        var environmentSettings = _environmentUtil.GetSettings(environment);
        PublishStatus(environmentSettings?.Name ?? environment, DeployWorkflowStatus.FromConclusion(conclusion),
            pullRequest.Number, run);
        if (environmentSettings == null || !ReportedConclusions.Contains(conclusion))
        {
            if (!string.IsNullOrEmpty(conclusion))
            {
                _logger.LogWarning("No deploy environment found for {Environment}.", environment);
            }

            return;
        }

        var settings = _context.Settings;
        var isFailure = conclusion == "failure";
        var isRollback = _pullRequestService.IsRollback(run);
        var body = _templateService.Render(
            isFailure
                ? TemplateService.DeployFailed
                : !isRollback
                    ? TemplateService.DeployCompleted
                    : TemplateService.RollbackCompleted,
            new
            {
                environment = environmentSettings,
                badge = settings.Badge,
                owner = _context.Owner,
                repo = _context.Repo,
                run = TemplateService.Run(run),
                isRollback
            });

        await _context.GitHub.Issue.Comment.Create(_context.Owner, _context.Repo, pullRequest.Number, body);

        var user = await _context.GitHub.User.Get(pullRequest.User.Login);
        var slackUser = await _slackService.LookupUserAsync(user);

        var builds = await _checkService.ListBuildsAsync(pullRequest);

        // Update what was deployed.
        var deployedServices = builds.Select(b => new DeployedService
        {
            Name = b.Name,
            RunId = b.RunId,
            Version = b.Version
        }).ToList();

        if (!isRollback)
        {
            // If this was successful, assume a sync has completed as well.
            await _deployStateService.SyncAsync(environment);
            await _deployStateService.UpdateAsync(environment, pullRequest.Number, deployedServices);
        }
        else
        {
            var rollbackCount = _pullRequestService.GetRollbackCount(run);
            var deployState = await _deployStateService.GetAsync(DeployStateService.StableEnvironment,
                rollbackCount - 1);
            await _deployStateService.UpdateAsync(environment, deployState.PullNumber, deployState.Services!);
        }

        // Pad build names to line up for display, in alphabetical order.
        var orderedBuilds = builds
            .Select(b => new { name = b.Name.PadRight(BuildNameMaxLength), runId = b.RunId, version = b.Version })
            .OrderBy(b => b.name, StringComparer.Ordinal)
            .ToList();

        var status = isFailure ? "`FAILED`" : "complete";
        var json = _templateService.Render(TemplateService.DeployCompletedJson, new
        {
            environment = environmentSettings,
            owner = _context.Owner,
            repo = _context.Repo,
            run = TemplateService.Run(run),
            pull = TemplateService.Pull(pullRequest),
            slackUser,
            status,
            builds = orderedBuilds,
            isRollback
        });

        await _slackService.PostMessageAsync(SlackChannel.Deploy, json);

        // Send release message when it is for a release environment.
        if (!string.Equals(environment, settings.ReleaseEnvironment, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Only post message for release if it is the first one for this pull request recently.
        _logger.LogInformation("The pull request {PullNumber} release message posting to slack.", pullRequest.Number);
        var cacheKey = $"{_context.Owner}:{_context.Repo}:released:{pullRequest.Number}";
        if (_cache.TryGetValue(cacheKey, out _))
        {
            _logger.LogInformation("The pull request {PullNumber} release has already been messaged.",
                pullRequest.Number);
            return;
        }

        var slackPullBody = _slackService.TranslateGitHubMarkdown(pullRequest.Body);
        json = _templateService.Render(TemplateService.DeployReleasedJson, new
        {
            environment = environmentSettings,
            owner = _context.Owner,
            repo = _context.Repo,
            run = TemplateService.Run(run),
            pull = TemplateService.Pull(pullRequest),
            slackUser,
            slackPullBody,
            status,
            builds = orderedBuilds,
            isRollback
        });
        await _slackService.PostMessageAsync(SlackChannel.Release, json);
        _cache.Set(cacheKey, true, PostReleaseMessageDelay);
    }

    /// <summary>
    /// Posts the deploy started to the configured destination.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="run">Object containing the run data used by the operation.</param>
    public async Task PostDeployStartedAsync(PullRequest pullRequest, WorkflowRun run)
    {
        // We need to retrieve the workflow run to get the correct title as it was not set yet.
        var updatedRun = await _context.GitHub.Actions.Workflows.Runs.Get(_context.Owner, _context.Repo, run.Id);
        var environment = _pullRequestService.GetEnvironment(pullRequest, updatedRun);
        _logger.LogInformation(
            "Posting deployment started message for {Environment} and pull {PullNumber}.\n    Run: {RunName}",
            environment, pullRequest.Number, updatedRun.Name);

        var environmentSettings = _environmentUtil.GetSettings(environment);
        if (environmentSettings == null)
        {
            _logger.LogWarning("No deploy environment found for {Environment}.", environment);
            return;
        }

        PublishStatus(environmentSettings.Name, DeployWorkflowState.InProgress, pullRequest.Number, run);

        var isRollback = _pullRequestService.IsRollback(updatedRun);
        var body = _templateService.Render(!isRollback ? TemplateService.DeployStarted : TemplateService.RollbackStarted,
            new
            {
                environment = environmentSettings,
                badge = _context.Settings.Badge,
                owner = _context.Owner,
                repo = _context.Repo,
                run = TemplateService.Run(run)
            });

        await _context.GitHub.Issue.Comment.Create(_context.Owner, _context.Repo, pullRequest.Number, body);

        if (environmentSettings.RequireApproval)
        {
            var user = await _context.GitHub.User.Get(pullRequest.User.Login);
            var slackUser = await _slackService.LookupUserAsync(user);

            var slackBody = _templateService.Render(TemplateService.EnvironmentApprovalRequiredJson, new
            {
                environment = environmentSettings,
                owner = _context.Owner,
                repo = _context.Repo,
                run = TemplateService.Run(run),
                slackUser,
                isRollback
            });

            await _slackService.PostMessageAsync(SlackChannel.Deploy, slackBody);
        }
    }

    /// <summary>
    /// Publishes the deploy workflow status to connected dashboards. Does nothing when the environment is unknown or
    /// the state is not a final outcome.
    /// </summary>
    /// <param name="environment">Environment name the workflow deploys to.</param>
    /// <param name="state">New state, or null when the run has no reportable state.</param>
    /// <param name="pullNumber">Pull request being deployed.</param>
    /// <param name="run">Workflow run the status comes from.</param>
    private void PublishStatus(string? environment, DeployWorkflowState? state, int pullNumber, WorkflowRun run)
    {
        if (string.IsNullOrEmpty(environment) || state == null)
        {
            return;
        }

        _broadcaster.Publish(new DeployWorkflowStatus(_context.Owner, _context.Repo, environment, state.Value,
            pullNumber, run.Id,
            !string.IsNullOrEmpty(run.HtmlUrl) ? run.HtmlUrl : $"{_context.RepositoryHtmlUrl}/actions/runs/{run.Id}",
            _timeProvider.GetUtcNow()));
    }
}

using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Webhooks.Managers;

/// <summary>
/// Reacts to workflow runs and closed pull requests to report deploy status and progress queues.
/// </summary>
public class StatusManager
{
    public const string RequestedAction = "requested";
    public const string CompletedAction = "completed";

    private readonly WebhookContext _context;
    private readonly StatusService _statusService;
    private readonly DeployService _deployService;
    private readonly PullRequestService _pullRequestService;
    private readonly AutomationTestService _automationTestService;
    private readonly ILogger<StatusManager> _logger;

    /// <summary>
    /// Initializes a status Manager with the supplied dependencies.
    /// </summary>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <param name="statusService">Value supplied for status Service.</param>
    /// <param name="deployService">Value supplied for deploy Service.</param>
    /// <param name="pullRequestService">Value supplied for pull Request Service.</param>
    /// <param name="automationTestService">Value supplied for automation Test Service.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public StatusManager(WebhookContext context, StatusService statusService, DeployService deployService,
        PullRequestService pullRequestService, AutomationTestService automationTestService,
        ILogger<StatusManager> logger)
    {
        _context = context;
        _statusService = statusService;
        _deployService = deployService;
        _pullRequestService = pullRequestService;
        _automationTestService = automationTestService;
        _logger = logger;
    }

    /// <summary>
    /// Processes the Workflow Run and applies the associated behavior.
    /// </summary>
    /// <param name="run">Value supplied for run.</param>
    /// <param name="action">Value supplied for action.</param>
    public async Task ProcessWorkflowRunAsync(WorkflowRun run, string action)
    {
        var pullNumber = run.PullRequests?.Count > 0 ? run.PullRequests[0].Number : 0;
        if (pullNumber == 0 && action == RequestedAction)
        {
            // Attempt to get updated workflow name.
            run = await _context.GitHub.Actions.Workflows.Runs.Get(_context.Owner, _context.Repo, run.Id);
        }

        if (pullNumber == 0 && _pullRequestService.IsRollback(run))
        {
            pullNumber = _pullRequestService.GetRollbackPullNumber(run);
        }

        if (pullNumber == 0)
        {
            return;
        }

        var pullRequest = await _pullRequestService.GetAsync(pullNumber);
        using var _ = _logger.BeginScope(new Dictionary<string, object> { ["PullNumber"] = pullNumber });

        if (IsDeployWorkflow(run.Path))
        {
            if (action == RequestedAction)
            {
                await _statusService.PostDeployStartedAsync(pullRequest, run);
            }
            else if (action == CompletedAction)
            {
                await _statusService.PostDeployCompletedAsync(pullRequest, run);

                if (run.Conclusion?.StringValue == "success")
                {
                    await _automationTestService.TriggerTestRunAsync(pullRequest, run);
                }
            }
        }
        else if (action == CompletedAction && !IsExcludedWorkflow(run.Path))
        {
            await _deployService.TryRetriggerDeployAsync(pullRequest);
        }
    }

    /// <summary>
    /// Processes the Pull Request Closed and applies the associated behavior.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    public async Task ProcessPullRequestClosedAsync(PullRequest pullRequest)
    {
        await _deployService.ReleasePullRequestAsync(pullRequest);
    }

    /// <summary>
    /// Determines whether the deploy Workflow condition holds.
    /// </summary>
    /// <param name="workflowPath">Value supplied for workflow Path.</param>
    /// <returns>The value produced by this operation.</returns>
    internal bool IsDeployWorkflow(string? workflowPath)
    {
        return !string.IsNullOrEmpty(workflowPath)
               && !string.IsNullOrEmpty(_context.Settings.DeployWorkflow)
               && string.Equals(Path.GetFileName(workflowPath), _context.Settings.DeployWorkflow,
                   StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the excluded Workflow condition holds.
    /// </summary>
    /// <param name="workflowPath">Value supplied for workflow Path.</param>
    /// <returns>The value produced by this operation.</returns>
    internal bool IsExcludedWorkflow(string? workflowPath)
    {
        var checkWorkflow = Path.GetFileName(workflowPath ?? string.Empty).ToLowerInvariant();
        return CheckService.GetExcludedWorkflows(_context.Settings).Contains(checkWorkflow);
    }
}

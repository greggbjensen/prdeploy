using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Webhooks.Managers;

/// <summary>Reacts to workflow runs and closed pull requests to report deploy status and progress queues.</summary>
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

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <param name="statusService">Object containing the status service data used by the operation.</param>
    /// <param name="deployService">Object containing the deploy service data used by the operation.</param>
    /// <param name="pullRequestService">Object containing the pull request service data used by the operation.</param>
    /// <param name="automationTestService">Object containing the automation test service data used by the operation.</param>
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

    /// <summary>Processes the workflow run and applies the associated behavior.</summary>
    /// <param name="run">Object containing the run data used by the operation.</param>
    /// <param name="action">Text used as the action.</param>
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

        if (pullNumber == 0 && IsDeployWorkflow(run.Path))
        {
            // Deploys are dispatched on the default branch, so GitHub links no pull request to the run.
            pullNumber = await _pullRequestService.ResolveDeployPullNumberAsync(run);
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

    /// <summary>Processes the pull request closed and applies the associated behavior.</summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    public async Task ProcessPullRequestClosedAsync(PullRequest pullRequest)
    {
        await _deployService.ReleasePullRequestAsync(pullRequest);
    }

    /// <summary>Determines whether the deploy workflow condition holds.</summary>
    /// <param name="workflowPath">Text used as the workflow path.</param>
    /// <returns>True when the requested condition is met; otherwise, false.</returns>
    internal bool IsDeployWorkflow(string? workflowPath)
    {
        return !string.IsNullOrEmpty(workflowPath)
               && !string.IsNullOrEmpty(_context.Settings.DeployWorkflow)
               && string.Equals(Path.GetFileName(workflowPath), Path.GetFileName(_context.Settings.DeployWorkflow),
                   StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Determines whether the excluded workflow condition holds.</summary>
    /// <param name="workflowPath">Text used as the workflow path.</param>
    /// <returns>True when the requested condition is met; otherwise, false.</returns>
    internal bool IsExcludedWorkflow(string? workflowPath)
    {
        var checkWorkflow = Path.GetFileName(workflowPath ?? string.Empty).ToLowerInvariant();
        return CheckService.GetExcludedWorkflows(_context.Settings).Contains(checkWorkflow);
    }
}

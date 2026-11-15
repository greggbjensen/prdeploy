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

    public async Task ProcessPullRequestClosedAsync(PullRequest pullRequest)
    {
        await _deployService.ReleasePullRequestAsync(pullRequest);
    }

    internal bool IsDeployWorkflow(string? workflowPath)
    {
        return !string.IsNullOrEmpty(workflowPath)
               && !string.IsNullOrEmpty(_context.Settings.DeployWorkflow)
               && string.Equals(Path.GetFileName(workflowPath), _context.Settings.DeployWorkflow,
                   StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsExcludedWorkflow(string? workflowPath)
    {
        var checkWorkflow = Path.GetFileName(workflowPath ?? string.Empty).ToLowerInvariant();
        return CheckService.GetExcludedWorkflows(_context.Settings).Contains(checkWorkflow);
    }
}

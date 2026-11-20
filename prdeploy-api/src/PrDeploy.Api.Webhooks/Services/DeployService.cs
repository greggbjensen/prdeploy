using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Webhooks.Models;
using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>
/// Handles deploy, rollback, free and queue operations for pull requests.
/// </summary>
public class DeployService
{
    private readonly WebhookContext _context;
    private readonly CheckService _checkService;
    private readonly TemplateService _templateService;
    private readonly SlackService _slackService;
    private readonly IDeploymentStore _deploymentStore;
    private readonly PullRequestService _pullRequestService;
    private readonly DeployStateService _deployStateService;
    private readonly EnvironmentUtil _environmentUtil;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DeployService> _logger;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <param name="checkService">Object containing the check service data used by the operation.</param>
    /// <param name="templateService">Object containing the template service data used by the operation.</param>
    /// <param name="slackService">Object containing the Slack service data used by the operation.</param>
    /// <param name="deploymentStore">Object containing the deployment store data used by the operation.</param>
    /// <param name="pullRequestService">Object containing the pull request service data used by the operation.</param>
    /// <param name="deployStateService">Object containing the deploy state service data used by the operation.</param>
    /// <param name="environmentUtil">Object containing the environment util data used by the operation.</param>
    /// <param name="timeProvider">Object containing the time provider data used by the operation.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public DeployService(WebhookContext context, CheckService checkService, TemplateService templateService,
        SlackService slackService, IDeploymentStore deploymentStore, PullRequestService pullRequestService,
        DeployStateService deployStateService, EnvironmentUtil environmentUtil, TimeProvider timeProvider,
        ILogger<DeployService> logger)
    {
        _context = context;
        _checkService = checkService;
        _templateService = templateService;
        _slackService = slackService;
        _deploymentStore = deploymentStore;
        _pullRequestService = pullRequestService;
        _deployStateService = deployStateService;
        _environmentUtil = environmentUtil;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the settings.
    /// </summary>
    /// <value>Current settings value.</value>
    private DeploySettings Settings => _context.Settings;
    /// <summary>
    /// Gets the git hub.
    /// </summary>
    /// <value>Current git hub value.</value>
    private IGitHubClient GitHub => _context.GitHub;

    /// <summary>
    /// Attempts to retrigger a deployment after the pull request checks complete; the result indicates whether it succeeded.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    public async Task TryRetriggerDeployAsync(PullRequest pullRequest)
    {
        var incompleteChecks = await _checkService.ListIncompletePullChecksAsync(pullRequest);
        if (incompleteChecks.Count > 0)
        {
            return;
        }

        var environment = _pullRequestService.GetEnvironment(pullRequest);
        if (string.IsNullOrEmpty(environment))
        {
            return;
        }

        environment = environment.ToLowerInvariant();
        _logger.LogInformation("Pull request {PullNumber} already deployed to {Environment}, redeploying.",
            pullRequest.Number, environment);

        var body = _templateService.Render(TemplateService.PullRequestChecksRedeploy, new
        {
            owner = _context.Owner,
            repo = _context.Repo,
            environment = _environmentUtil.GetSettings(environment),
            badge = Settings.Badge,
            prdeployPortalUrl = Settings.PrdeployPortalUrl
        });

        await GitHub.Issue.Comment.Create(_context.Owner, _context.Repo, pullRequest.Number, body);

        await TriggerDeployAsync(pullRequest, environment, retainLocks: true);
    }

    /// <summary>
    /// Requests the deploy from the associated service.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    /// <param name="force">Whether the force option is enabled.</param>
    /// <param name="retainLocks">Whether the retain locks option is enabled.</param>
    public async Task RequestDeployAsync(PullRequest pullRequest, string? environment, long commentId, bool force,
        bool retainLocks)
    {
        await AddCommentReactionAsync(commentId, ReactionType.Rocket);

        var defaultedEnvironment = _pullRequestService.GetDefaultedEnvironment(pullRequest, environment);
        if (force)
        {
            _logger.LogInformation("Force deploying PR {PullNumber} to {Environment}.", pullRequest.Number,
                defaultedEnvironment);
            await TriggerDeployAsync(pullRequest, defaultedEnvironment, retainLocks);
            return;
        }

        var normalizedEnvironment = _environmentUtil.Normalize(defaultedEnvironment);
        _logger.LogInformation("Trying queue deploy PR {PullNumber} to {Environment}.", pullRequest.Number,
            normalizedEnvironment);

        var availableEnvironment = await FindAvailableEnvironmentForQueueAsync(normalizedEnvironment,
            pullRequest.Number);
        if (!string.IsNullOrEmpty(availableEnvironment))
        {
            await TriggerDeployAsync(pullRequest, availableEnvironment, retainLocks);
        }
        else
        {
            await EnqueuePullRequestAsync(pullRequest, normalizedEnvironment, retainLocks);
        }
    }

    /// <summary>
    /// Releases the pull request for other operations.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    public async Task ReleasePullRequestAsync(PullRequest pullRequest)
    {
        _logger.LogInformation("Freeing pull request {PullNumber} after close.", pullRequest.Number);

        var availableEnvironments = await _pullRequestService.FreeAsync(pullRequest);
        await RemovePullRequestFromQueuesAsync(pullRequest);
        await DequeuePullRequestsAsync(availableEnvironments);

        var environment = _pullRequestService.GetEnvironment(pullRequest);
        if (pullRequest.Merged && !string.IsNullOrEmpty(environment)
                               && string.Equals(environment, Settings.ReleaseEnvironment,
                                   StringComparison.OrdinalIgnoreCase))
        {
            // Promote deploy state from released to stable.
            var deployState = await _deployStateService.GetAsync(environment);
            await _deployStateService.UpdateAsync(DeployStateService.StableEnvironment, pullRequest.Number,
                deployState.Services!);
        }
    }

    /// <summary>
    /// Restores the deploy to a previous state.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    /// <param name="rollbackCount">Numeric value used for the rollback count.</param>
    public async Task RollbackDeployAsync(PullRequest pullRequest, string? environment, long commentId,
        int rollbackCount)
    {
        await AddCommentReactionAsync(commentId, ReactionType.Confused);

        // If no environment is specified, rollback all assigned environments.
        var rollbackEnvironments = _pullRequestService.GetAllEnvironments(pullRequest, environment);
        if (rollbackEnvironments.Count == 1)
        {
            _logger.LogInformation("Rolling back pull request.");

            var rollbackEnvironment = rollbackEnvironments[0];
            await _pullRequestService.FreeAsync(pullRequest, rollbackEnvironment);
            await TriggerDeployAsync(pullRequest, rollbackEnvironment, retainLocks: false, rollback: true,
                rollbackCount);
        }
        else
        {
            _logger.LogWarning("Cannot rollback multiple environments.");
            await PostDeployFailedAsync(pullRequest.Number, environment, rollback: true,
                "You cannot rollback multiple environments.  You must specify an environment to rollback.");
        }
    }

    /// <summary>
    /// Releases the deploy so it can be used again.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    public async Task FreeDeployAsync(PullRequest pullRequest, string? environment, long commentId)
    {
        await AddCommentReactionAsync(commentId, ReactionType.Eyes);

        var availableEnvironments = await _pullRequestService.FreeAsync(pullRequest, environment);
        await RemovePullRequestFromQueuesAsync(pullRequest);
        await DequeuePullRequestsAsync(availableEnvironments);
    }

    /// <summary>
    /// Adds the services to the associated collection.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="services">Services to include in the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    public async Task AddServicesAsync(PullRequest pullRequest, IReadOnlyCollection<string> services,
        long commentId)
    {
        await AddCommentReactionAsync(commentId, ReactionType.Plus1);

        // Add a file to the service folder and append a datestamp and branch name to trigger a build.
        var servicesNotFound = new List<string>();
        foreach (var service in services)
        {
            // Do not run in parallel to avoid GitHub limits.
            var serviceSettings = (Settings.Services ?? [])
                .Find(s => string.Equals(s.Name, service, StringComparison.OrdinalIgnoreCase));
            if (serviceSettings == null)
            {
                servicesNotFound.Add(service);
                continue;
            }

            var path = $"{serviceSettings.Path}/.deploy";

            // Keep the history from the default branch, but the update needs the file sha on the branch.
            var content = (await TryGetFileAsync(path, _context.DefaultBranch))?.Content ?? string.Empty;
            var sha = (await TryGetFileAsync(path, pullRequest.Head.Ref))?.Sha;

            content = content.Trim();
            if (content.Length > 0)
            {
                content += "\n";
            }

            var dateValue = _timeProvider.GetUtcNow().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            content += $"{dateValue} {pullRequest.Head.Ref}\n";
            var message = $"Adding {service} to pull request build.";

            if (sha == null)
            {
                _logger.LogInformation("Creating file to add to deploy: {Path}", path);
                await GitHub.Repository.Content.CreateFile(_context.Owner, _context.Repo, path,
                    new CreateFileRequest(message, content, pullRequest.Head.Ref));
            }
            else
            {
                _logger.LogInformation("Updating file to add to deploy: {Path}", path);
                await GitHub.Repository.Content.UpdateFile(_context.Owner, _context.Repo, path,
                    new UpdateFileRequest(message, content, sha, pullRequest.Head.Ref));
            }
        }

        if (servicesNotFound.Count > 0)
        {
            var body = _templateService.Render(TemplateService.ServicesNotFound, new
            {
                badge = Settings.Badge,
                services = servicesNotFound
            });

            await GitHub.Issue.Comment.Create(_context.Owner, _context.Repo, pullRequest.Number, body);
        }
    }

    /// <summary>
    /// Attempts to get file; the result indicates whether it succeeded.
    /// </summary>
    /// <param name="path">Path used to locate the resource.</param>
    /// <param name="reference">Text used as the reference.</param>
    /// <returns>The repository content produced by the operation.</returns>
    private async Task<RepositoryContent?> TryGetFileAsync(string path, string reference)
    {
        try
        {
            var contents = await GitHub.Repository.Content.GetAllContentsByRef(_context.Owner, _context.Repo,
                path, reference);
            return contents.FirstOrDefault();
        }
        catch (NotFoundException)
        {
            _logger.LogDebug("File {Path} not found at {Reference} in {Owner}/{Repo}.", path, reference,
                _context.Owner, _context.Repo);
            return null;
        }
    }

    /// <summary>
    /// Removes the pull request from queues from the associated collection.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="keepEnvironment">Text used as the keep environment.</param>
    private async Task RemovePullRequestFromQueuesAsync(PullRequest pullRequest, string keepEnvironment = "")
    {
        var queueGroups = (Settings.Environments ?? []).GroupBy(e => e.Queue);
        var tasks = queueGroups.Select(async group =>
        {
            var normalizedEnvironment = _environmentUtil.Normalize(group.First().Name);
            if (normalizedEnvironment == keepEnvironment)
            {
                return;
            }

            var pullNumbers = await GetQueuePullNumbersAsync(normalizedEnvironment);
            var updatedPullNumbers = pullNumbers.Where(n => n != pullRequest.Number).ToList();

            // If queue has changed, update it.
            if (updatedPullNumbers.Count != pullNumbers.Count)
            {
                _logger.LogInformation("Removing pull request {PullNumber} from {Environment} queue.",
                    pullRequest.Number, normalizedEnvironment);
                await SetQueuePullNumbersAsync(normalizedEnvironment, updatedPullNumbers);
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Performs the enqueue pull request operation.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="normalizedEnvironment">Deployment environment involved in the operation.</param>
    /// <param name="retainLocks">Whether the retain locks option is enabled.</param>
    private async Task EnqueuePullRequestAsync(PullRequest pullRequest, string normalizedEnvironment,
        bool retainLocks)
    {
        var pullNumber = pullRequest.Number;
        _logger.LogInformation("Enqueuing pull request {PullNumber} for {Environment}.", pullNumber,
            normalizedEnvironment);

        var updateQueue = true;
        var alreadyInQueue = false;
        int queuePosition;
        var queuePullNumbers = await GetQueuePullNumbersAsync(normalizedEnvironment);
        var index = queuePullNumbers.IndexOf(pullNumber);
        if (index == -1)
        {
            queuePullNumbers.Add(pullNumber);
            queuePosition = queuePullNumbers.Count;
        }
        else
        {
            queuePosition = index + 1;
            _logger.LogInformation("Pull request already in queue at position {Position}.", queuePosition);
            updateQueue = false;
            alreadyInQueue = true;
        }

        if (updateQueue)
        {
            await SetQueuePullNumbersAsync(normalizedEnvironment, queuePullNumbers);

            // Make sure the pull request is not in any other queues.
            await RemovePullRequestFromQueuesAsync(pullRequest, normalizedEnvironment);
        }

        if (!retainLocks)
        {
            // Unlock environments since it is in the queue now for the next environment.
            var availableEnvironments = await _pullRequestService.FreeAsync(pullRequest);
            await DequeuePullRequestsAsync(availableEnvironments);
        }

        var queueTable = _templateService.RenderQueueTable(_context.Owner, _context.Repo, normalizedEnvironment,
            queuePullNumbers, _context.RepositoryHtmlUrl, Settings.PrdeployPortalUrl);

        var body = _templateService.Render(TemplateService.PullRequestEnqueued, new
        {
            owner = _context.Owner,
            repo = _context.Repo,
            environment = _environmentUtil.GetSettings(normalizedEnvironment),
            badge = Settings.Badge,
            prdeployPortalUrl = Settings.PrdeployPortalUrl,
            queuePosition,
            queueTable,
            alreadyInQueue
        });

        await GitHub.Issue.Comment.Create(_context.Owner, _context.Repo, pullNumber, body);
    }

    /// <summary>
    /// Performs the trigger deploy operation.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="retainLocks">Whether the retain locks option is enabled.</param>
    /// <param name="rollback">Whether the rollback option is enabled.</param>
    /// <param name="rollbackCount">Numeric value used for the rollback count.</param>
    private async Task TriggerDeployAsync(PullRequest pullRequest, string environment, bool retainLocks,
        bool rollback = false, int rollbackCount = 1)
    {
        _logger.LogInformation("Triggering deployment for {PullNumber} in {Environment}.  Rollback: {Rollback}",
            pullRequest.Number, environment, rollback);

        var deployPullRequest = pullRequest;
        if (rollback)
        {
            var deployState = await _deployStateService.GetAsync(DeployStateService.StableEnvironment,
                rollbackCount - 1);
            _logger.LogInformation("Rolling back to pull request {RollbackPullNumber} for {Environment}.",
                deployState.PullNumber, environment);
            if (deployState.PullNumber == 0)
            {
                await PostDeployFailedAsync(pullRequest.Number, environment, rollback,
                    "Could not find stable state with a previous pull request number.  You must have at least one successful deploy.");
                return;
            }

            deployPullRequest = await _pullRequestService.GetAsync(deployState.PullNumber);
        }

        _logger.LogDebug("Removing pull request from queues for {Environment}.", environment);
        var availableEnvironments = await _pullRequestService.ProgressEnvironmentAsync(deployPullRequest,
            environment, retainLocks);
        await RemovePullRequestFromQueuesAsync(pullRequest);

        // Remove any other pull requests in this environment.
        _logger.LogDebug("Removing other pull requests in {Environment}.", environment);
        var pulls = await _pullRequestService.ListDeployedAsync();
        var pullsWithEnvironment = _pullRequestService.ListWithEnvironment(pulls, environment,
            deployPullRequest.Number);
        foreach (var environmentPull in pullsWithEnvironment)
        {
            _logger.LogDebug("Removing pull request {PullNumber} from {Environment}.", environmentPull.Number,
                environment);
            await _pullRequestService.RemoveEnvironmentAsync(environmentPull, environment);
        }

        if (!rollback)
        {
            await DequeuePullRequestsAsync(availableEnvironments);
        }

        // Allow labels to be set even if checks fail, so it can be retriggered.
        if (!await _checkService.VerifyDeployReadyAsync(pullRequest, environment))
        {
            return;
        }

        _logger.LogDebug("Building pull request title.");
        var commits = await GitHub.PullRequest.Commits(_context.Owner, _context.Repo, pullRequest.Number);
        var firstCommitLine = commits.FirstOrDefault()?.Commit.Message.Split('\n')[0] ?? string.Empty;
        var environmentText = !rollback
            ? environment
            : rollbackCount == 1
                ? $"{environment} ROLLBACK #{pullRequest.Number}"
                : $"{environment} ROLLBACK #{pullRequest.Number} {rollbackCount}";
        var deployName = $"[{environmentText}] {pullRequest.Head.Ref}: {firstCommitLine}";

        await TriggerDeployWorkflowAsync(deployName, environment, pullRequest, deployPullRequest, rollback,
            rollbackCount);
    }

    /// <summary>
    /// Performs the trigger deploy workflow operation.
    /// </summary>
    /// <param name="deployName">Text used as the deploy name.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="triggeringPullRequest">Object containing the triggering pull request data used by the operation.</param>
    /// <param name="deployPullRequest">Object containing the deploy pull request data used by the operation.</param>
    /// <param name="rollback">Whether the rollback option is enabled.</param>
    /// <param name="rollbackCount">Numeric value used for the rollback count.</param>
    private async Task TriggerDeployWorkflowAsync(string deployName, string environment,
        PullRequest triggeringPullRequest, PullRequest? deployPullRequest, bool rollback, int rollbackCount)
    {
        _logger.LogDebug("Triggering deploy: {DeployName}", deployName);

        // If PR branch does not exist, use default.  This is for rollback on closed PRs.
        var branch = deployPullRequest?.Head?.Ref;
        if (!string.IsNullOrEmpty(branch))
        {
            try
            {
                await GitHub.Repository.Branch.Get(_context.Owner, _context.Repo, branch);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to find branch {Branch} in {Owner}/{Repo}; using {DefaultBranch}.",
                    branch, _context.Owner, _context.Repo, _context.DefaultBranch);
                branch = _context.DefaultBranch;
            }
        }
        else
        {
            branch = _context.DefaultBranch;
        }

        List<DeployedService> deployServices;
        List<DeployedService> syncServices;

        if (!rollback)
        {
            if (deployPullRequest != null)
            {
                var builds = await _checkService.ListBuildsAsync(deployPullRequest);
                deployServices = builds.Select(ToDeployedService).ToList();
            }
            else
            {
                deployServices = [];
            }

            // Stable is a special environment for merged production releases.
            var diff = await _deployStateService.DiffAsync(DeployStateService.StableEnvironment, environment);

            // Filter out things already being deployed.
            syncServices = diff.Services!.Where(s => deployServices.All(d => d.Name != s.Name)).ToList();
        }
        else
        {
            // If this is a rollback we want to show all differences as deploys.
            var diff = await _deployStateService.DiffAsync(DeployStateService.StableEnvironment, environment,
                rollbackCount - 1);

            var excludeFromRollback = _environmentUtil.GetSettings(environment)?.ExcludeFromRollback ?? [];
            var excludedServices = diff.Services!.Where(s => excludeFromRollback.Contains(s.Name))
                .Select(s => s.Name);
            _logger.LogInformation("Services excluded from rollback: {Services}", string.Join(", ", excludedServices));

            deployServices = diff.Services!.Where(s => !excludeFromRollback.Contains(s.Name)).ToList();
            syncServices = [];
        }

        var deployRunIds = deployServices.Select(s => s.RunId).ToList();
        var syncRunIds = syncServices.Select(s => s.RunId).ToList();
        _logger.LogInformation(
            "Triggering {Workflow} with:\n    Deploy Run IDs: {DeployRunIds}\n    Sync Run IDs:   {SyncRunIds}",
            Settings.DeployWorkflow, string.Join(",", deployRunIds), string.Join(",", syncRunIds));

        if (deployRunIds.Count == 0)
        {
            await PostNoDeploymentsFoundAsync(triggeringPullRequest, environment);
            return;
        }

        try
        {
            await GitHub.Actions.Workflows.CreateDispatch(_context.Owner, _context.Repo, Settings.DeployWorkflow,
                new CreateWorkflowDispatch(branch)
                {
                    Inputs = new Dictionary<string, object>
                    {
                        ["pull_number"] = deployPullRequest?.Number.ToString() ?? string.Empty,
                        ["environment"] = environment,
                        ["deploy_name"] = deployName,
                        ["deploy_run_ids"] = string.Join(",", deployRunIds),
                        ["sync_run_ids"] = string.Join(",", syncRunIds)
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to dispatch {Workflow}.", Settings.DeployWorkflow);
            await PostDeployFailedAsync(triggeringPullRequest.Number, environment, rollback, ex.Message);
        }
    }

    /// <summary>
    /// Converts the supplied value to the deployed service.
    /// </summary>
    /// <param name="build">Object containing the build data used by the operation.</param>
    /// <returns>The deployed service produced by the operation.</returns>
    private static DeployedService ToDeployedService(Build build) => new()
    {
        Name = build.Name,
        RunId = build.RunId,
        Version = build.Version
    };

    /// <summary>
    /// Posts the deploy failed to the configured destination.
    /// </summary>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="rollback">Whether the rollback option is enabled.</param>
    /// <param name="message">Text used as the message.</param>
    private async Task PostDeployFailedAsync(int pullNumber, string? environment, bool rollback, string message)
    {
        var body = _templateService.Render(TemplateService.DeployFailed, new
        {
            environment = _environmentUtil.GetSettings(environment),
            badge = Settings.Badge,
            owner = _context.Owner,
            repo = _context.Repo,
            run = new { },
            isRollback = rollback,
            message
        });

        await GitHub.Issue.Comment.Create(_context.Owner, _context.Repo, pullNumber, body);
    }

    /// <summary>
    /// Posts the no deployments found to the configured destination.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    private async Task PostNoDeploymentsFoundAsync(PullRequest pullRequest, string environment)
    {
        var environmentSettings = _environmentUtil.GetSettings(environment);

        var body = _templateService.Render(TemplateService.DeployNothingFound, new
        {
            environment = environmentSettings,
            badge = Settings.Badge,
            owner = _context.Owner,
            repo = _context.Repo
        });

        await GitHub.Issue.Comment.Create(_context.Owner, _context.Repo, pullRequest.Number, body);

        var user = await GitHub.User.Get(pullRequest.User.Login);
        var slackUser = await _slackService.LookupUserAsync(user);

        var json = _templateService.Render(TemplateService.DeployNothingFoundJson, new
        {
            environment = environmentSettings,
            owner = _context.Owner,
            repo = _context.Repo,
            pull = TemplateService.Pull(pullRequest),
            slackUser
        });

        await _slackService.PostMessageAsync(SlackChannel.Deploy, json);
    }

    /// <summary>
    /// Adds the comment reaction to the associated collection.
    /// </summary>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    /// <param name="reaction">Object containing the reaction data used by the operation.</param>
    private async Task AddCommentReactionAsync(long commentId, ReactionType reaction)
    {
        await GitHub.Reaction.IssueComment.Create(_context.Owner, _context.Repo, commentId, new NewReaction(reaction));
    }

    /// <summary>
    /// Finds and returns the available environment for queue.
    /// </summary>
    /// <param name="normalizedEnvironment">Deployment environment involved in the operation.</param>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <returns>Items matching the supplied criteria.</returns>
    private async Task<string> FindAvailableEnvironmentForQueueAsync(string normalizedEnvironment, int pullNumber)
    {
        var availableEnvironment = string.Empty;
        var pulls = await _pullRequestService.ListDeployedAsync();

        var existingPull = pulls.FirstOrDefault(p => p.Number == pullNumber);
        if (existingPull != null)
        {
            var existingEnvironment = _pullRequestService.GetAllEnvironments(existingPull, normalizedEnvironment,
                normalized: true);
            if (existingEnvironment.Count > 0)
            {
                availableEnvironment = existingEnvironment[0];
                _logger.LogInformation("Pull request {PullNumber} is already in {Environment}, reusing.", pullNumber,
                    availableEnvironment);
            }
        }

        // Find new available environment.
        if (string.IsNullOrEmpty(availableEnvironment))
        {
            var usedEnvironments = _pullRequestService.GetUsedEnvironments(pulls, normalizedEnvironment, pullNumber);

            // Find all environments associated with the same queue.
            var environmentSettings = _environmentUtil.GetRequiredSettings(normalizedEnvironment);
            var queueEnvironments = (Settings.Environments ?? [])
                .Where(e => string.Equals(e.Queue, environmentSettings.Queue, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Name.ToLowerInvariant())
                .ToList();
            var availableEnvironments = queueEnvironments.Where(n => !usedEnvironments.Contains(n)).ToList();

            _logger.LogInformation("Environments {Queue} queue:\n    Searched: {Searched}\n    Available: {Available}",
                environmentSettings.Queue, string.Join(", ", queueEnvironments),
                string.Join(", ", availableEnvironments));

            if (availableEnvironments.Count > 0)
            {
                availableEnvironment = availableEnvironments[0];
            }
        }

        return availableEnvironment;
    }

    /// <summary>
    /// Performs the dequeue pull requests operation.
    /// </summary>
    /// <param name="normalizedEnvironments">Object containing the normalized environments data used by the operation.</param>
    private async Task DequeuePullRequestsAsync(IReadOnlyCollection<string> normalizedEnvironments)
    {
        if (normalizedEnvironments.Count == 0)
        {
            return;
        }

        await Task.WhenAll(normalizedEnvironments.Select(DequeuePullRequestAsync));
    }

    /// <summary>
    /// Performs the dequeue pull request operation.
    /// </summary>
    /// <param name="normalizedEnvironment">Deployment environment involved in the operation.</param>
    private async Task DequeuePullRequestAsync(string normalizedEnvironment)
    {
        var pullNumbers = await GetQueuePullNumbersAsync(normalizedEnvironment);
        PullRequest? nextPullRequest = null;
        if (pullNumbers.Count > 0)
        {
            nextPullRequest = await _pullRequestService.GetAsync(pullNumbers[0]);
            await TriggerDeployAsync(nextPullRequest, normalizedEnvironment, retainLocks: true);
        }

        await PostEnvironmentAvailableMessageAsync(normalizedEnvironment, nextPullRequest);
    }

    /// <summary>
    /// Posts the environment available message to the configured destination.
    /// </summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="takenPullRequest">Object containing the taken pull request data used by the operation.</param>
    private async Task PostEnvironmentAvailableMessageAsync(string environment, PullRequest? takenPullRequest)
    {
        var takenMessage = takenPullRequest != null
            ? $" but taken from queue by <{takenPullRequest.HtmlUrl}|#{takenPullRequest.Number}>"
            : string.Empty;

        var json = _templateService.Render(TemplateService.EnvironmentAvailableJson, new
        {
            environment = _environmentUtil.GetSettings(environment),
            takenMessage
        });
        await _slackService.PostMessageAsync(SlackChannel.Deploy, json);
    }

    /// <summary>
    /// Retrieves the queue pull numbers.
    /// </summary>
    /// <param name="normalizedEnvironment">Deployment environment involved in the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    private async Task<List<int>> GetQueuePullNumbersAsync(string normalizedEnvironment)
    {
        var queueName = _environmentUtil.GetRequiredSettings(normalizedEnvironment).Queue;
        var pullNumbers = await _deploymentStore.GetAsync<List<int>?>(_context.Owner, _context.Repo, queueName);
        if (pullNumbers == null)
        {
            pullNumbers = [];
            await _deploymentStore.SetAsync(_context.Owner, _context.Repo, queueName, pullNumbers);
        }

        return pullNumbers;
    }

    /// <summary>
    /// Stores or updates the queue pull numbers.
    /// </summary>
    /// <param name="normalizedEnvironment">Deployment environment involved in the operation.</param>
    /// <param name="pullNumbers">Object containing the pull numbers data used by the operation.</param>
    private async Task SetQueuePullNumbersAsync(string normalizedEnvironment, List<int> pullNumbers)
    {
        var queueName = _environmentUtil.GetRequiredSettings(normalizedEnvironment).Queue;
        await _deploymentStore.SetAsync(_context.Owner, _context.Repo, queueName, pullNumbers);

        _logger.LogInformation("Queue for {Environment} updated: {PullNumbers}", normalizedEnvironment,
            string.Join(", ", pullNumbers));
    }
}

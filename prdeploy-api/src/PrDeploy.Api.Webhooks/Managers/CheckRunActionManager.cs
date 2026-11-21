using Microsoft.Extensions.Logging;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Webhooks.Managers;

/// <summary>Runs the deploy, redeploy and free buttons pressed on a pull request's prdeploy check run.</summary>
/// <remarks>
/// GitHub sends <c>check_run.requested_action</c> only to the app that created the check run. Buttons are held to the
/// same rules as comment commands: only users with write access can use them, and pull requests from forks cannot
/// be deployed.
/// </remarks>
public class CheckRunActionManager
{
    private readonly DeployService _deployService;
    private readonly PullRequestService _pullRequestService;
    private readonly DeployCheckTracker _checkTracker;
    private readonly ILogger<CheckRunActionManager> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="deployService">Runs deploys and frees environments.</param>
    /// <param name="pullRequestService">Reads pull requests and checks the user's permission.</param>
    /// <param name="checkTracker">Records the pull request so its check run is refreshed after the action.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public CheckRunActionManager(DeployService deployService, PullRequestService pullRequestService,
        DeployCheckTracker checkTracker, ILogger<CheckRunActionManager> logger)
    {
        _deployService = deployService;
        _pullRequestService = pullRequestService;
        _checkTracker = checkTracker;
        _logger = logger;
    }

    /// <summary>Runs the action behind a pressed check run button.</summary>
    /// <param name="checkName">Name of the check run the button is on; only the prdeploy check is handled.</param>
    /// <param name="identifier">Identifier of the pressed button.</param>
    /// <param name="externalId">External ID of the check run, which holds the pull request number.</param>
    /// <param name="pullNumbers">Pull requests GitHub links to the check run, used when the external ID is missing.</param>
    /// <param name="sender">Login of the user who pressed the button.</param>
    public async Task ProcessRequestedActionAsync(string? checkName, string? identifier, string? externalId,
        IReadOnlyList<int> pullNumbers, string? sender)
    {
        if (checkName != DeployCheckService.CheckName)
        {
            _logger.LogDebug("Ignoring requested action on check {CheckName}.", checkName);
            return;
        }

        var pullNumber = int.TryParse(externalId, out var parsed) && parsed > 0
            ? parsed
            : pullNumbers.FirstOrDefault();
        if (pullNumber <= 0)
        {
            _logger.LogWarning("Unable to find the pull request for requested action {Action}.", identifier);
            return;
        }

        using var _ = _logger.BeginScope(new Dictionary<string, object> { ["PullNumber"] = pullNumber });
        if (!await _pullRequestService.CanWriteAsync(sender))
        {
            _logger.LogWarning("Ignoring {Action} button from {Sender}, who does not have write access.", identifier,
                sender);
            return;
        }

        var pullRequest = await _pullRequestService.GetAsync(pullNumber);
        var deploys = identifier is DeployCheckService.DeployAction or DeployCheckService.RedeployAction;
        if (deploys && !_pullRequestService.IsFromThisRepository(pullRequest))
        {
            _logger.LogWarning("Ignoring {Action} button for a pull request from {HeadRepository}.", identifier,
                pullRequest.Head?.Repository?.FullName ?? "a deleted repository");
            return;
        }

        _logger.LogInformation("Running {Action} button from {Sender}.", identifier, sender);
        switch (identifier)
        {
            case DeployCheckService.DeployAction:
                await _deployService.RequestDeployAsync(pullRequest, environment: null, commentId: 0, force: false,
                    retainLocks: false);
                break;

            case DeployCheckService.RedeployAction:
                // Redeploy the environment the pull request holds, which reuses it instead of queueing.
                var environment = _pullRequestService.GetEnvironment(pullRequest);
                await _deployService.RequestDeployAsync(pullRequest, string.IsNullOrEmpty(environment) ? null : environment,
                    commentId: 0, force: false, retainLocks: false);
                break;

            case DeployCheckService.FreeAction:
                await _deployService.FreeDeployAsync(pullRequest, environment: null, commentId: 0);
                break;

            default:
                _logger.LogWarning("Ignoring unknown requested action {Action}.", identifier);
                return;
        }

        _checkTracker.Track(pullNumber);
    }
}

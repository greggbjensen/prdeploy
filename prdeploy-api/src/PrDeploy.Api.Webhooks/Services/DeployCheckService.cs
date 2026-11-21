using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Webhooks.Options;
using PrDeploy.Api.Webhooks.Status;
using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>Publishes a <c>prdeploy</c> check run on each pull request's head commit that shows its environments, queue position, deploy status and services, with buttons for the next deploy action.</summary>
/// <remarks>
/// The check is in progress while the pull request deploys, queued while it waits in a deployment queue, failed when
/// its latest deploy failed, successful when it deployed, and neutral otherwise, so it can be made a required status
/// check. Pressing a button sends a <c>check_run.requested_action</c> webhook handled by
/// <see cref="Managers.CheckRunActionManager"/>. Publishing is best effort: GitHub errors are logged and never stop
/// the webhook flow.
/// </remarks>
public class DeployCheckService
{
    /// <summary>Name of the check run, shown in the pull request checks list.</summary>
    public const string CheckName = "prdeploy";

    /// <summary>Button identifier that requests a deploy to the default environment.</summary>
    public const string DeployAction = "deploy";

    /// <summary>Button identifier that redeploys the environment the pull request holds.</summary>
    public const string RedeployAction = "redeploy";

    /// <summary>Button identifier that frees the pull request's environments and removes it from queues.</summary>
    public const string FreeAction = "free";

    private const int ButtonLabelMaxLength = 20;
    private const int ButtonDescriptionMaxLength = 40;
    private const int MaxPageSize = 100;

    private readonly WebhookContext _context;
    private readonly PullRequestService _pullRequestService;
    private readonly EnvironmentUtil _environmentUtil;
    private readonly IDeploymentStore _deploymentStore;
    private readonly DeployStatusBroadcaster _broadcaster;
    private readonly DeployCheckTracker _tracker;
    private readonly TimeProvider _timeProvider;
    private readonly GitHubAppOptions _appOptions;
    private readonly ILogger<DeployCheckService> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="context">Webhook context holding the installation client, repository and settings.</param>
    /// <param name="pullRequestService">Reads pull requests and their environment labels.</param>
    /// <param name="environmentUtil">Normalizes environment names.</param>
    /// <param name="deploymentStore">Holds the deployment queues.</param>
    /// <param name="broadcaster">Holds the latest deploy workflow status of each environment.</param>
    /// <param name="tracker">Pull requests whose deploy state changed during the current event.</param>
    /// <param name="timeProvider">Supplies the completion time of completed check runs.</param>
    /// <param name="appOptions">GitHub App options; the app ID limits updates to check runs this app created.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public DeployCheckService(WebhookContext context, PullRequestService pullRequestService,
        EnvironmentUtil environmentUtil, IDeploymentStore deploymentStore, DeployStatusBroadcaster broadcaster,
        DeployCheckTracker tracker, TimeProvider timeProvider, IOptions<GitHubAppOptions> appOptions,
        ILogger<DeployCheckService> logger)
    {
        _context = context;
        _pullRequestService = pullRequestService;
        _environmentUtil = environmentUtil;
        _deploymentStore = deploymentStore;
        _broadcaster = broadcaster;
        _tracker = tracker;
        _timeProvider = timeProvider;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    /// <summary>Publishes the check run of every pull request recorded by <see cref="DeployCheckTracker"/> during this event.</summary>
    public async Task PublishTrackedAsync()
    {
        foreach (var pullNumber in _tracker.Drain())
        {
            try
            {
                // Labels change while an event is processed, so the latest pull request is read.
                var pullRequest = await _pullRequestService.GetAsync(pullNumber);
                await PublishAsync(pullRequest);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "Unable to publish the {CheckName} check for pull request {PullNumber} ({StatusCode}).",
                    CheckName, pullNumber, ex.StatusCode);
            }
        }
    }

    /// <summary>Creates or updates the check run on a pull request's head commit from its current deploy state.</summary>
    /// <param name="pullRequest">Pull request whose state is published.</param>
    public async Task PublishAsync(PullRequest pullRequest)
    {
        var headSha = pullRequest.Head?.Sha;
        if (string.IsNullOrEmpty(headSha))
        {
            return;
        }

        var view = BuildView(pullRequest, await GetQueuePositionsAsync(pullRequest.Number),
            await ListServicesAsync(headSha));
        var output = new NewCheckRunOutput(view.Title, view.Summary);
        var completedAt = view.Conclusion != null ? _timeProvider.GetUtcNow() : (DateTimeOffset?)null;
        var detailsUrl = GetDetailsUrl(pullRequest);

        var existing = await FindExistingAsync(headSha);

        // A completed check run cannot be moved back to queued or in progress, so a new one replaces it.
        if (existing != null && (existing.Status.Value != CheckStatus.Completed || view.Conclusion != null))
        {
            await _context.GitHub.Check.Run.Update(_context.Owner, _context.Repo, existing.Id, new CheckRunUpdate
            {
                Status = view.Status,
                Conclusion = view.Conclusion,
                CompletedAt = completedAt,
                Output = output,
                Actions = view.Actions,
                DetailsUrl = detailsUrl,
                ExternalId = pullRequest.Number.ToString()
            });
            _logger.LogDebug("Updated the {CheckName} check {CheckRunId} for pull request {PullNumber}: {Title}.",
                CheckName, existing.Id, pullRequest.Number, view.Title);
            return;
        }

        await _context.GitHub.Check.Run.Create(_context.Owner, _context.Repo, new NewCheckRun(CheckName, headSha)
        {
            Status = view.Status,
            Conclusion = view.Conclusion,
            CompletedAt = completedAt,
            Output = output,
            Actions = view.Actions,
            DetailsUrl = detailsUrl,
            ExternalId = pullRequest.Number.ToString()
        });
        _logger.LogDebug("Created the {CheckName} check for pull request {PullNumber}: {Title}.", CheckName,
            pullRequest.Number, view.Title);
    }

    /// <summary>Works out the check run status, title, summary and buttons for a pull request.</summary>
    /// <param name="pullRequest">Pull request whose labels hold its environments.</param>
    /// <param name="queuePositions">Queues the pull request waits in, as normalized environment to position and length.</param>
    /// <param name="services">Configured services and whether the pull request's head commit triggered their builds.</param>
    /// <returns>The view to publish.</returns>
    internal DeployCheckView BuildView(PullRequest pullRequest,
        IReadOnlyDictionary<string, (int Position, int Count)> queuePositions,
        IReadOnlyList<(string Name, bool Triggered)> services)
    {
        var labels = (pullRequest.Labels ?? []).Select(l => l.Name.ToLowerInvariant()).ToHashSet();
        var configured = (_context.Settings.Environments ?? []).Select(e => e.Name.ToLowerInvariant()).ToList();
        var locked = _pullRequestService.GetAllEnvironments(pullRequest);
        var deployed = configured.Where(labels.Contains).ToList();
        var statuses = _broadcaster.GetSnapshot(_context.Owner, _context.Repo)
            .Where(s => s.PullNumber == pullRequest.Number)
            .ToDictionary(s => s.Environment.ToLowerInvariant(), s => s);

        var primary = locked.Count > 0 ? _pullRequestService.GetEnvironment(pullRequest) : string.Empty;
        CheckStatus status;
        CheckConclusion? conclusion;
        string title;
        if (!string.IsNullOrEmpty(primary))
        {
            (status, conclusion, title) = statuses.TryGetValue(primary, out var primaryStatus)
                ? primaryStatus.State switch
                {
                    DeployWorkflowState.InProgress => (CheckStatus.InProgress, (CheckConclusion?)null, $"Deploying to {primary}"),
                    DeployWorkflowState.Successful => (CheckStatus.Completed, CheckConclusion.Success, $"Deployed to {primary}"),
                    _ => (CheckStatus.Completed, CheckConclusion.Failure, $"Deploy to {primary} failed")
                }
                : (CheckStatus.Completed, CheckConclusion.Neutral, $"Holding {primary}");
        }
        else if (queuePositions.Count > 0)
        {
            var (queue, (position, count)) = queuePositions.OrderBy(q => q.Key, StringComparer.Ordinal).First();
            (status, conclusion, title) = (CheckStatus.Queued, null, $"Queued for {queue} ({position} of {count})");
        }
        else
        {
            (status, conclusion, title) = (CheckStatus.Completed, CheckConclusion.Neutral,
                deployed.Count > 0 ? $"Freed {string.Join(", ", deployed)}" : "Not deployed");
        }

        return new DeployCheckView(status, conclusion, title,
            BuildSummary(locked, deployed, statuses, queuePositions, services),
            BuildActions(pullRequest, primary, queuePositions.Count > 0));
    }

    /// <summary>Renders the check run summary as GitHub Flavored Markdown tables of environments, queues and services.</summary>
    /// <param name="locked">Environments the pull request holds the lock for.</param>
    /// <param name="deployed">Environments labelled with the pull request, locked or not.</param>
    /// <param name="statuses">Latest deploy workflow status of the pull request in each environment.</param>
    /// <param name="queuePositions">Queues the pull request waits in.</param>
    /// <param name="services">Configured services and whether the pull request triggered their builds.</param>
    /// <returns>The Markdown summary.</returns>
    private string BuildSummary(IReadOnlyCollection<string> locked, IReadOnlyCollection<string> deployed,
        IReadOnlyDictionary<string, DeployWorkflowStatus> statuses,
        IReadOnlyDictionary<string, (int Position, int Count)> queuePositions,
        IReadOnlyList<(string Name, bool Triggered)> services)
    {
        var summary = new StringBuilder();
        summary.AppendLine("### Environments").AppendLine();
        if (deployed.Count == 0)
        {
            summary.AppendLine("This pull request is not deployed to any environment.");
        }
        else
        {
            summary.AppendLine("| Environment | Lock | Deploy |").AppendLine("|---|---|---|");
            foreach (var environment in deployed)
            {
                var lockText = locked.Contains(environment) ? "🔒 Locked" : "Freed";
                var deployText = !statuses.TryGetValue(environment, out var status)
                    ? "—"
                    : string.IsNullOrEmpty(status.RunUrl)
                        ? FormatState(status.State)
                        : $"[{FormatState(status.State)}]({status.RunUrl})";
                summary.AppendLine($"| `{environment}` | {lockText} | {deployText} |");
            }
        }

        if (queuePositions.Count > 0)
        {
            summary.AppendLine().AppendLine("### Queues").AppendLine();
            foreach (var (queue, (position, count)) in queuePositions.OrderBy(q => q.Key, StringComparer.Ordinal))
            {
                summary.AppendLine($"- Position **{position}** of {count} in the [`{queue}` queue]({PortalUrl($"deployments?environment={queue}")}).");
            }
        }

        if (services.Count > 0)
        {
            summary.AppendLine().AppendLine("### Services").AppendLine()
                .AppendLine("| Service | Built by this pull request |").AppendLine("|---|---|");
            foreach (var (name, triggered) in services.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            {
                summary.AppendLine($"| `{name}` | {(triggered ? "✅ Yes" : "—")} |");
            }
        }

        summary.AppendLine().Append(
            "Use the buttons above, or comment `/deploy`, `/free`, `/rollback` or `/add` on the pull request.");
        var portal = PortalUrl("deployments");
        if (portal != null)
        {
            summary.Append($" See every environment in the [prdeploy portal]({portal}).");
        }

        return summary.AppendLine().ToString();
    }

    /// <summary>Chooses the buttons shown on the check run for the pull request's current state.</summary>
    /// <param name="pullRequest">Pull request the buttons act on.</param>
    /// <param name="primary">Environment the pull request holds, or empty when it holds none.</param>
    /// <param name="queued">Whether the pull request waits in a queue.</param>
    /// <returns>Up to three buttons, the most GitHub shows.</returns>
    private List<NewCheckRunAction> BuildActions(PullRequest pullRequest, string primary, bool queued)
    {
        // Pull requests from forks cannot be deployed by branch name, so they only get a button to free locks.
        var canDeploy = _pullRequestService.IsFromThisRepository(pullRequest);
        var actions = new List<NewCheckRunAction>();
        if (!string.IsNullOrEmpty(primary))
        {
            if (canDeploy)
            {
                actions.Add(Action($"Redeploy {primary}", $"Deploy the latest builds to {primary}.", RedeployAction));
            }

            actions.Add(Action("Free", "Release the environment locks.", FreeAction));
        }
        else if (queued)
        {
            actions.Add(Action("Leave queue", "Remove the pull request from the queue.", FreeAction));
        }
        else if (canDeploy)
        {
            var environment = _context.Settings.DefaultEnvironment ?? "dev";
            actions.Add(Action($"Deploy to {environment}", $"Deploy or queue for {environment}.", DeployAction));
        }

        return actions;
    }

    /// <summary>Lists the queues a pull request waits in and its position in each.</summary>
    /// <param name="pullNumber">Pull request to look for.</param>
    /// <returns>Normalized environment of each queue the pull request is in, with its 1-based position and the queue length.</returns>
    private async Task<IReadOnlyDictionary<string, (int Position, int Count)>> GetQueuePositionsAsync(int pullNumber)
    {
        var positions = new Dictionary<string, (int, int)>();
        foreach (var group in (_context.Settings.Environments ?? []).GroupBy(e => e.Queue))
        {
            var pullNumbers = await _deploymentStore.GetAsync<List<int>?>(_context.Owner, _context.Repo, group.Key)
                              ?? [];
            var index = pullNumbers.IndexOf(pullNumber);
            if (index >= 0)
            {
                positions[_environmentUtil.Normalize(group.First().Name)] = (index + 1, pullNumbers.Count);
            }
        }

        return positions;
    }

    /// <summary>Lists the configured services and whether a commit triggered their build workflows.</summary>
    /// <param name="headSha">Head commit of the pull request.</param>
    /// <returns>Each configured service; empty when none are configured or the runs cannot be read.</returns>
    private async Task<IReadOnlyList<(string Name, bool Triggered)>> ListServicesAsync(string headSha)
    {
        var services = _context.Settings.Services ?? [];
        if (services.Count == 0)
        {
            return [];
        }

        try
        {
            var runs = await _context.GitHub.Actions.Workflows.Runs.List(_context.Owner, _context.Repo,
                new WorkflowRunsRequest { HeadSha = headSha },
                new ApiOptions { PageSize = MaxPageSize, PageCount = 1, StartPage = 1 });
            var workflows = (runs?.WorkflowRuns ?? [])
                .Select(r => Path.GetFileName(r.Path ?? string.Empty))
                .Where(w => w.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return services
                .Select(s => (s.Name, !string.IsNullOrEmpty(s.Workflow) && workflows.Contains(Path.GetFileName(s.Workflow))))
                .ToList();
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "Unable to list workflow runs for {HeadSha}; the {CheckName} check omits services.",
                headSha, CheckName);
            return [];
        }
    }

    /// <summary>Finds the newest prdeploy check run on a commit.</summary>
    /// <param name="headSha">Commit the check run is attached to.</param>
    /// <returns>The check run, or null when this app has not created one for the commit.</returns>
    private async Task<CheckRun?> FindExistingAsync(string headSha)
    {
        var response = await _context.GitHub.Check.Run.GetAllForReference(_context.Owner, _context.Repo, headSha,
            new CheckRunRequest { CheckName = CheckName },
            new ApiOptions { PageSize = MaxPageSize, PageCount = 1, StartPage = 1 });

        var appId = long.TryParse(_appOptions.AppId, out var id) ? id : (long?)null;
        return (response?.CheckRuns ?? [])
            .Where(c => c.Name == CheckName && (appId == null || c.App?.Id == appId))
            .OrderByDescending(c => c.Id)
            .FirstOrDefault();
    }

    /// <summary>Gets the link opened by the check run's details link: the portal deployments page, or the pull request.</summary>
    /// <param name="pullRequest">Pull request the check belongs to.</param>
    /// <returns>An absolute https URL, or null when neither is available.</returns>
    private string? GetDetailsUrl(PullRequest pullRequest)
    {
        var url = PortalUrl("deployments") ?? pullRequest.HtmlUrl;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? url : null;
    }

    /// <summary>Builds a link to a page of the repository in the prdeploy portal.</summary>
    /// <param name="path">Path below the repository, such as <c>deployments</c>.</param>
    /// <returns>The link, or null when no portal URL is configured.</returns>
    private string? PortalUrl(string path)
    {
        var portal = _context.Settings.PrdeployPortalUrl?.TrimEnd('/');
        return string.IsNullOrEmpty(portal) ? null : $"{portal}/{_context.Owner}/{_context.Repo}/{path}";
    }

    /// <summary>Creates a check run button, shortening text to the lengths GitHub accepts.</summary>
    /// <param name="label">Button text.</param>
    /// <param name="description">Tooltip text.</param>
    /// <param name="identifier">Identifier sent back in the requested action webhook.</param>
    /// <returns>The button.</returns>
    private static NewCheckRunAction Action(string label, string description, string identifier) =>
        new(Truncate(label, ButtonLabelMaxLength), Truncate(description, ButtonDescriptionMaxLength), identifier);

    /// <summary>Shortens text to a maximum length.</summary>
    /// <param name="text">Text to shorten.</param>
    /// <param name="maxLength">Maximum number of characters.</param>
    /// <returns>The text, cut when it is too long.</returns>
    private static string Truncate(string text, int maxLength) => text.Length <= maxLength ? text : text[..maxLength];

    /// <summary>Describes a deploy workflow state for the summary table.</summary>
    /// <param name="state">State to describe.</param>
    /// <returns>Short text with an icon.</returns>
    private static string FormatState(DeployWorkflowState state) => state switch
    {
        DeployWorkflowState.InProgress => "⏳ In progress",
        DeployWorkflowState.Successful => "✅ Succeeded",
        _ => "❌ Failed"
    };
}

/// <summary>The status, text and buttons published on a pull request's prdeploy check run.</summary>
/// <param name="Status">Check run status.</param>
/// <param name="Conclusion">Conclusion when the status is completed; otherwise null.</param>
/// <param name="Title">Short title shown next to the check name.</param>
/// <param name="Summary">Markdown summary shown on the check run page.</param>
/// <param name="Actions">Buttons shown on the check run.</param>
public sealed record DeployCheckView(CheckStatus Status, CheckConclusion? Conclusion, string Title, string Summary,
    IReadOnlyList<NewCheckRunAction> Actions);

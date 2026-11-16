using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Webhooks.Managers;

/// <summary>
/// Runs slash commands such as /deploy, /free, /rollback and /add from pull request comments.
/// </summary>
public partial class CommentCommandManager
{
    private readonly DeployService _deployService;
    private readonly PullRequestService _pullRequestService;
    private readonly ILogger<CommentCommandManager> _logger;
    private readonly List<CommentCommand> _commands;

    /// <summary>
    /// Initializes a comment Command Manager with the supplied dependencies.
    /// </summary>
    /// <param name="deployService">Value supplied for deploy Service.</param>
    /// <param name="pullRequestService">Value supplied for pull Request Service.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public CommentCommandManager(DeployService deployService, PullRequestService pullRequestService,
        ILogger<CommentCommandManager> logger)
    {
        _deployService = deployService;
        _pullRequestService = pullRequestService;
        _logger = logger;
        _commands =
        [
            new("deploy", DeployRegex(), DeployCommandAsync),
            new("free", FreeRegex(), FreeCommandAsync),
            new("rollback", RollbackRegex(), RollbackCommandAsync),
            new("add", AddRegex(), AddCommandAsync)
        ];
    }

    /// <summary>
    /// Processes the Comment and applies the associated behavior.
    /// </summary>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    /// <param name="body">Value supplied for body.</param>
    /// <param name="issueUrl">Value supplied for issue Url.</param>
    public async Task ProcessCommentAsync(long commentId, string? body, string? issueUrl)
    {
        foreach (var command in _commands)
        {
            var commandMatch = command.Pattern.Match(body ?? string.Empty);
            if (!commandMatch.Success)
            {
                continue;
            }

            var issueMatch = IssueIdRegex().Match(issueUrl ?? string.Empty);
            if (!issueMatch.Success)
            {
                _logger.LogWarning("Unable to find pull request number in {IssueUrl}.", issueUrl);
                return;
            }

            var pullNumber = int.Parse(issueMatch.Groups["issueNumber"].Value);
            var pullRequest = await _pullRequestService.GetAsync(pullNumber);
            using var _ = _logger.BeginScope(new Dictionary<string, object> { ["PullNumber"] = pullNumber });
            _logger.LogInformation("Running {Command} command.", command.Name);
            await command.Action(commandMatch, pullRequest, commentId);
            break;
        }
    }

    /// <summary>
    /// Performs the deploy Command operation.
    /// </summary>
    /// <param name="match">Value supplied for match.</param>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    private async Task DeployCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var environment = GetGroup(match, "environment");
        var forceDeploy = string.Equals(GetGroup(match, "forceDeploy"), "force", StringComparison.OrdinalIgnoreCase);
        var retainLocks = string.Equals(GetGroup(match, "retainLocks"), "retain", StringComparison.OrdinalIgnoreCase);

        await _deployService.RequestDeployAsync(pullRequest, environment, commentId, forceDeploy, retainLocks);
    }

    /// <summary>
    /// Restores the Command to a previous state.
    /// </summary>
    /// <param name="match">Value supplied for match.</param>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    private async Task RollbackCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var environment = GetGroup(match, "environment");
        var count = int.TryParse(GetGroup(match, "count"), out var parsedCount) ? parsedCount : 1;

        await _deployService.RollbackDeployAsync(pullRequest, environment, commentId, count);
    }

    /// <summary>
    /// Releases the Command so it can be used again.
    /// </summary>
    /// <param name="match">Value supplied for match.</param>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    private async Task FreeCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var environment = GetGroup(match, "environment");

        await _deployService.FreeDeployAsync(pullRequest, environment, commentId);
    }

    /// <summary>
    /// Adds the Command to the associated collection.
    /// </summary>
    /// <param name="match">Value supplied for match.</param>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    private async Task AddCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var services = match.Groups["services"].Value
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        await _deployService.AddServicesAsync(pullRequest, services, commentId);
    }

    /// <summary>
    /// Retrieves the Group.
    /// </summary>
    /// <param name="match">Value supplied for match.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string? GetGroup(Match match, string name)
    {
        var group = match.Groups[name];
        return group.Success ? group.Value : null;
    }

    /// <summary>
    /// Performs the deploy Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"(?:^|\s)/deploy(?:\s+(?<environment>\w+))?(?:\s+--(?<forceDeploy>force))?(?:\s+--(?<retainLocks>retain))?")]
    private static partial Regex DeployRegex();

    /// <summary>
    /// Releases the Regex so it can be used again.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"(?:^|\s)/free(?:\s+(?<environment>\w+))?")]
    private static partial Regex FreeRegex();

    /// <summary>
    /// Restores the Regex to a previous state.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"(?:^|\s)/rollback(?:\s+(?<environment>\w+))?(?:\s+(?<count>\d+))?")]
    private static partial Regex RollbackRegex();

    /// <summary>
    /// Adds the Regex to the associated collection.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"(?:^|\s)/add\s+(?<services>[\w\-\s]+)")]
    private static partial Regex AddRegex();

    /// <summary>
    /// Determines whether the issue Id Regex condition holds.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@".*/(?<issueNumber>\d+)")]
    private static partial Regex IssueIdRegex();

    /// <summary>
    /// Represents the comment Command used by the application.
    /// </summary>
    /// <param name="Name">Name used to identify the value or resource.</param>
    /// <param name="Pattern">Value supplied for pattern.</param>
    /// <param name="Action">Value supplied for action.</param>
    private record CommentCommand(string Name, Regex Pattern, Func<Match, PullRequest, long, Task> Action);
}

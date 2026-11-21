using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Webhooks.Managers;

/// <summary>Runs slash commands such as /deploy, /free, /rollback and /add from pull request comments.</summary>
public partial class CommentCommandManager
{
    private readonly DeployService _deployService;
    private readonly PullRequestService _pullRequestService;
    private readonly ILogger<CommentCommandManager> _logger;
    private readonly List<CommentCommand> _commands;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="deployService">Object containing the deploy service data used by the operation.</param>
    /// <param name="pullRequestService">Object containing the pull request service data used by the operation.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public CommentCommandManager(DeployService deployService, PullRequestService pullRequestService,
        ILogger<CommentCommandManager> logger)
    {
        _deployService = deployService;
        _pullRequestService = pullRequestService;
        _logger = logger;
        _commands =
        [
            new("deploy", DeployRegex(), DeployCommandAsync, RequiresBranchInRepository: true),
            new("free", FreeRegex(), FreeCommandAsync, RequiresBranchInRepository: false),
            new("rollback", RollbackRegex(), RollbackCommandAsync, RequiresBranchInRepository: false),
            new("add", AddRegex(), AddCommandAsync, RequiresBranchInRepository: true)
        ];
    }

    /// <summary>Processes the comment and applies the associated behavior.</summary>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    /// <param name="body">Text used as the body.</param>
    /// <param name="issueUrl">Text used as the issue URL.</param>
    /// <param name="author">Login of the comment author. Commands only run for users with write access, because
    /// anyone who can read a repository (or any GitHub user, on a public one) can comment on its pull requests.</param>
    public async Task ProcessCommentAsync(long commentId, string? body, string? issueUrl, string? author)
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
            using var _ = _logger.BeginScope(new Dictionary<string, object> { ["PullNumber"] = pullNumber });
            if (!await _pullRequestService.CanWriteAsync(author))
            {
                _logger.LogWarning("Ignoring {Command} command from {Author}, who does not have write access.",
                    command.Name, author);
                return;
            }

            var pullRequest = await _pullRequestService.GetAsync(pullNumber);
            if (command.RequiresBranchInRepository && !_pullRequestService.IsFromThisRepository(pullRequest))
            {
                _logger.LogWarning("Ignoring {Command} command for a pull request from {HeadRepository}.",
                    command.Name, pullRequest.Head?.Repository?.FullName ?? "a deleted repository");
                return;
            }

            _logger.LogInformation("Running {Command} command from {Author}.", command.Name, author);
            await command.Action(commandMatch, pullRequest, commentId);
            break;
        }
    }

    /// <summary>Performs the deploy command operation.</summary>
    /// <param name="match">Object containing the match data used by the operation.</param>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    private async Task DeployCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var environment = GetGroup(match, "environment");
        var forceDeploy = string.Equals(GetGroup(match, "forceDeploy"), "force", StringComparison.OrdinalIgnoreCase);
        var retainLocks = string.Equals(GetGroup(match, "retainLocks"), "retain", StringComparison.OrdinalIgnoreCase);

        await _deployService.RequestDeployAsync(pullRequest, environment, commentId, forceDeploy, retainLocks);
    }

    /// <summary>Restores the command to a previous state.</summary>
    /// <param name="match">Object containing the match data used by the operation.</param>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    private async Task RollbackCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var environment = GetGroup(match, "environment");
        var count = int.TryParse(GetGroup(match, "count"), out var parsedCount) ? parsedCount : 1;

        await _deployService.RollbackDeployAsync(pullRequest, environment, commentId, count);
    }

    /// <summary>Releases the command so it can be used again.</summary>
    /// <param name="match">Object containing the match data used by the operation.</param>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    private async Task FreeCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var environment = GetGroup(match, "environment");

        await _deployService.FreeDeployAsync(pullRequest, environment, commentId);
    }

    /// <summary>Adds the command to the associated collection.</summary>
    /// <param name="match">Object containing the match data used by the operation.</param>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="commentId">Identifier of the GitHub comment associated with the request.</param>
    private async Task AddCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var services = match.Groups["services"].Value
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        await _deployService.AddServicesAsync(pullRequest, services, commentId);
    }

    /// <summary>Retrieves the group.</summary>
    /// <param name="match">Object containing the match data used by the operation.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    private static string? GetGroup(Match match, string name)
    {
        var group = match.Groups[name];
        return group.Success ? group.Value : null;
    }

    /// <summary>Performs the deploy regex operation and returns its result.</summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"(?:^|\s)/deploy(?:\s+(?<environment>\w+))?(?:\s+--(?<forceDeploy>force))?(?:\s+--(?<retainLocks>retain))?")]
    private static partial Regex DeployRegex();

    /// <summary>Releases the regex so it can be used again.</summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"(?:^|\s)/free(?:\s+(?<environment>\w+))?")]
    private static partial Regex FreeRegex();

    /// <summary>Restores the regex to a previous state.</summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"(?:^|\s)/rollback(?:\s+(?<environment>\w+))?(?:\s+(?<count>\d+))?")]
    private static partial Regex RollbackRegex();

    /// <summary>Adds the regex to the associated collection.</summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"(?:^|\s)/add\s+(?<services>[\w\-\s]+)")]
    private static partial Regex AddRegex();

    /// <summary>Determines whether the issue id regex condition holds.</summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@".*/(?<issueNumber>\d+)")]
    private static partial Regex IssueIdRegex();

    /// <summary>Describes a supported pull-request comment command and the handler that processes it.</summary>
    /// <param name="Name">Command name used in logs and user-facing feedback.</param>
    /// <param name="Pattern">Regular expression that recognizes the command and captures its arguments.</param>
    /// <param name="Action">Asynchronous handler invoked with the match, pull request, and comment identifier.</param>
    /// <param name="RequiresBranchInRepository">Whether the command deploys or commits to the pull request branch by name,
    /// so it must be refused for pull requests from forks.</param>
    private record CommentCommand(string Name, Regex Pattern, Func<Match, PullRequest, long, Task> Action,
        bool RequiresBranchInRepository);
}

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

    private async Task DeployCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var environment = GetGroup(match, "environment");
        var forceDeploy = string.Equals(GetGroup(match, "forceDeploy"), "force", StringComparison.OrdinalIgnoreCase);
        var retainLocks = string.Equals(GetGroup(match, "retainLocks"), "retain", StringComparison.OrdinalIgnoreCase);

        await _deployService.RequestDeployAsync(pullRequest, environment, commentId, forceDeploy, retainLocks);
    }

    private async Task RollbackCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var environment = GetGroup(match, "environment");
        var count = int.TryParse(GetGroup(match, "count"), out var parsedCount) ? parsedCount : 1;

        await _deployService.RollbackDeployAsync(pullRequest, environment, commentId, count);
    }

    private async Task FreeCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var environment = GetGroup(match, "environment");

        await _deployService.FreeDeployAsync(pullRequest, environment, commentId);
    }

    private async Task AddCommandAsync(Match match, PullRequest pullRequest, long commentId)
    {
        var services = match.Groups["services"].Value
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        await _deployService.AddServicesAsync(pullRequest, services, commentId);
    }

    private static string? GetGroup(Match match, string name)
    {
        var group = match.Groups[name];
        return group.Success ? group.Value : null;
    }

    [GeneratedRegex(@"(?:^|\s)/deploy(?:\s+(?<environment>\w+))?(?:\s+--(?<forceDeploy>force))?(?:\s+--(?<retainLocks>retain))?")]
    private static partial Regex DeployRegex();

    [GeneratedRegex(@"(?:^|\s)/free(?:\s+(?<environment>\w+))?")]
    private static partial Regex FreeRegex();

    [GeneratedRegex(@"(?:^|\s)/rollback(?:\s+(?<environment>\w+))?(?:\s+(?<count>\d+))?")]
    private static partial Regex RollbackRegex();

    [GeneratedRegex(@"(?:^|\s)/add\s+(?<services>[\w\-\s]+)")]
    private static partial Regex AddRegex();

    [GeneratedRegex(@".*/(?<issueNumber>\d+)")]
    private static partial Regex IssueIdRegex();

    private record CommentCommand(string Name, Regex Pattern, Func<Match, PullRequest, long, Task> Action);
}

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Webhooks.Managers;

/// <summary>
/// Adds links to the Jira issues found in the branch name to the top of the pull request body.
/// </summary>
public partial class JiraManager
{
    private readonly JiraService _jiraService;
    private readonly PullRequestService _pullRequestService;
    private readonly TemplateService _templateService;
    private readonly ILogger<JiraManager> _logger;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="jiraService">Object containing the Jira service data used by the operation.</param>
    /// <param name="pullRequestService">Object containing the pull request service data used by the operation.</param>
    /// <param name="templateService">Object containing the template service data used by the operation.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public JiraManager(JiraService jiraService, PullRequestService pullRequestService,
        TemplateService templateService, ILogger<JiraManager> logger)
    {
        _jiraService = jiraService;
        _pullRequestService = pullRequestService;
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Adds the Jira issues to pull request to the associated collection.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    public async Task AddJiraIssuesToPullRequestAsync(PullRequest pullRequest)
    {
        var branchName = pullRequest.Head.Ref;
        var pullBody = pullRequest.Body ?? string.Empty;

        // Issues for this branch were already added, which also stops the edit this makes from looping.
        // Make backward compatible before branch was in body.
        foreach (Match match in IssuesTextRegex().Matches(pullBody))
        {
            var matchBranch = match.Groups["branchName"];
            if (!matchBranch.Success || matchBranch.Value == branchName)
            {
                _logger.LogInformation("No body changes found.");
                return;
            }
        }

        var issues = await _jiraService.ListAssociatedIssuesAsync(branchName);
        pullBody = IssuesTextRegex().Replace(pullBody, string.Empty).TrimStart();

        var body = _templateService.Render(TemplateService.PullRequestIssues, new { issues, pullBody, branchName })
            .Trim();
        await _pullRequestService.UpdatePullRequestBodyAsync(pullRequest.Number, body);
    }

    /// <summary>
    /// Determines whether the issues text regex condition holds.
    /// </summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"<!-- ISSUES_START(?::(?<branchName>.*?))? .*? ISSUES_END -->", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex IssuesTextRegex();
}

using System.Net;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Managers;

namespace PrDeploy.Api.Tests.Webhooks.Managers;

/// <summary>
/// Contains tests that verify the behavior of the jira Manager Test.
/// </summary>
public class JiraManagerTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly JiraManager _manager;
    private readonly List<string> _bodies = [];

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    public JiraManagerTest()
    {
        _manager = _host.Get<JiraManager>();
        _host.Http.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                { "issues": [{ "key": "SCRUM-1234", "self": "https://myorg.atlassian.net/rest/api/3/issue/1",
                  "fields": { "summary": "Add the thing", "issuetype": { "name": "Story" } } }] }
                """)
        };
        _host.MockOf(_host.GitHub.Object.PullRequest)
            .Setup(c => c.Update(WebhookTestHost.Owner, WebhookTestHost.Repo, 3, It.IsAny<PullRequestUpdate>()))
            .Callback<string, string, int, PullRequestUpdate>((_, _, _, u) => _bodies.Add(u.Body))
            .ReturnsAsync((PullRequest)null!);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: adds Issues To Top Of Body.
    /// </summary>
    [Fact]
    public async Task AddJiraIssuesToPullRequestAsync_AddsIssuesToTopOfBody()
    {
        await _manager.AddJiraIssuesToPullRequestAsync(
            GitHubModels.PullRequest(3, "SCRUM-1234-thing", body: "My description"));

        Assert.Equal("""
            <!-- ISSUES_START:SCRUM-1234-thing -->
            Story: [SCRUM-1234](https://myorg.atlassian.net/browse/SCRUM-1234) Add the thing
            <!-- ISSUES_END -->

            My description
            """.Replace("\r", ""), Assert.Single(_bodies));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: does Nothing When Issues Already Added For Branch.
    /// </summary>
    [Fact]
    public async Task AddJiraIssuesToPullRequestAsync_DoesNothingWhenIssuesAlreadyAddedForBranch()
    {
        await _manager.AddJiraIssuesToPullRequestAsync(GitHubModels.PullRequest(3, "SCRUM-1234-thing",
            body: "<!-- ISSUES_START:SCRUM-1234-thing -->\nStory: x\n<!-- ISSUES_END -->\n\nMy description"));

        Assert.Empty(_bodies);
        Assert.Empty(_host.Http.Requests);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: replaces Issues When Branch Changed.
    /// </summary>
    [Fact]
    public async Task AddJiraIssuesToPullRequestAsync_ReplacesIssuesWhenBranchChanged()
    {
        await _manager.AddJiraIssuesToPullRequestAsync(GitHubModels.PullRequest(3, "SCRUM-1234-thing",
            body: "<!-- ISSUES_START:SCRUM-9-old -->\nStory: old\n<!-- ISSUES_END -->\n\nMy description"));

        var body = Assert.Single(_bodies);
        Assert.DoesNotContain("SCRUM-9", body);
        Assert.EndsWith("\n\nMy description", body);
    }

    /// <summary>
    /// Releases resources owned by this instance.
    /// </summary>
    public void Dispose() => _host.Dispose();
}

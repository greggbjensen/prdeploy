using System.Net;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Managers;

namespace PrDeploy.Api.Tests.Webhooks.Managers;

public class JiraManagerTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly JiraManager _manager;
    private readonly List<string> _bodies = [];

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

    [Fact]
    public async Task AddJiraIssuesToPullRequestAsync_DoesNothingWhenIssuesAlreadyAddedForBranch()
    {
        await _manager.AddJiraIssuesToPullRequestAsync(GitHubModels.PullRequest(3, "SCRUM-1234-thing",
            body: "<!-- ISSUES_START:SCRUM-1234-thing -->\nStory: x\n<!-- ISSUES_END -->\n\nMy description"));

        Assert.Empty(_bodies);
        Assert.Empty(_host.Http.Requests);
    }

    [Fact]
    public async Task AddJiraIssuesToPullRequestAsync_ReplacesIssuesWhenBranchChanged()
    {
        await _manager.AddJiraIssuesToPullRequestAsync(GitHubModels.PullRequest(3, "SCRUM-1234-thing",
            body: "<!-- ISSUES_START:SCRUM-9-old -->\nStory: old\n<!-- ISSUES_END -->\n\nMy description"));

        var body = Assert.Single(_bodies);
        Assert.DoesNotContain("SCRUM-9", body);
        Assert.EndsWith("\n\nMy description", body);
    }

    public void Dispose() => _host.Dispose();
}

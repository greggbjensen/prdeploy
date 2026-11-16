using System.Net;
using System.Text.Json;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>
/// Contains tests that verify Jira service behavior.
/// </summary>
public class JiraServiceTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly JiraService _service;

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    public JiraServiceTest()
    {
        _service = _host.Get<JiraService>();
        _host.Http.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                  "issues": [
                    {
                      "key": "SCRUM-1234",
                      "self": "https://greggbjensen.atlassian.net/rest/api/3/issue/10001",
                      "fields": {
                        "summary": "Containerize www.myorg.com for the dev environment",
                        "issuetype": { "name": "Story" }
                      }
                    }
                  ]
                }
                """)
        };
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: handles no associated issues.
    /// </summary>
    [Fact]
    public async Task ListAssociatedIssuesAsync_HandlesNoAssociatedIssues()
    {
        var result = await _service.ListAssociatedIssuesAsync("app-k8s-deploy");

        Assert.Empty(result);
        Assert.Empty(_host.Http.Requests);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: lists associated issues.
    /// </summary>
    [Fact]
    public async Task ListAssociatedIssuesAsync_ListsAssociatedIssues()
    {
        var issues = await _service.ListAssociatedIssuesAsync("SCRUM-1234-scrum-1242-UX-553-app-k8s-deploy");

        var issue = Assert.Single(issues);
        Assert.Equal("SCRUM-1234", issue.Key);
        Assert.Equal("Story", issue.Type);
        Assert.Equal("https://greggbjensen.atlassian.net/browse/SCRUM-1234", issue.Url);

        var (request, body) = Assert.Single(_host.Http.Requests);
        Assert.Equal("https://myorg.atlassian.net/rest/api/3/search/jql", request.RequestUri!.ToString());
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        using var json = JsonDocument.Parse(body!);
        Assert.Equal("key in ('SCRUM-1234', 'SCRUM-1242', 'UX-553')", json.RootElement.GetProperty("jql").GetString());
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: returns empty when Jira fails.
    /// </summary>
    [Fact]
    public async Task ListAssociatedIssuesAsync_ReturnsEmptyWhenJiraFails()
    {
        _host.Http.Respond = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized);

        Assert.Empty(await _service.ListAssociatedIssuesAsync("SCRUM-1234-feature"));
    }

    /// <summary>
    /// Releases resources owned by this instance.
    /// </summary>
    public void Dispose() => _host.Dispose();
}

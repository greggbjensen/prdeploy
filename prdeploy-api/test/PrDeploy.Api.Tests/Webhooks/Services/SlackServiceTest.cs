using System.Net;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Models;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Services;

public class SlackServiceTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly SlackService _service;

    public SlackServiceTest()
    {
        _service = _host.Get<SlackService>();
        _host.Http.Respond = request => request.RequestUri!.Host == "slack.com"
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "ok": true, "user": { "id": "U789745", "name": "dhivya" } }""")
            }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
    }

    [Fact]
    public async Task PostMessageAsync_PostsMessageToChannel()
    {
        const string json = """{ "blocks": [{ "type": "section", "text": { "type": "plain_text", "text": "Test" } }] }""";
        await _service.PostMessageAsync(SlackChannel.Release, json);

        var (request, body) = Assert.Single(_host.Http.Requests);
        Assert.Equal("https://hooks.slack.com/release", request.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(json, body);
    }

    [Fact]
    public async Task PostMessageAsync_DoesNotPostWhenDisabled()
    {
        _host.Context.Settings.Slack!.NotificationsEnabled = false;
        await _service.PostMessageAsync(SlackChannel.Deploy, "{}");

        Assert.Empty(_host.Http.Requests);
    }

    [Fact]
    public async Task LookupUserAsync_GetsSlackUserByBuiltEmailFromName()
    {
        var user = await _service.LookupUserAsync(User(name: "Dhivya Middle Selvam"));

        Assert.NotNull(user);
        Assert.Equal("U789745", user.Id);
        Assert.Equal("dhivya", user.Username);
        var (request, _) = Assert.Single(_host.Http.Requests);
        Assert.Equal("https://slack.com/api/users.lookupByEmail?email=dhivya.selvam%40myorg.com",
            request.RequestUri!.ToString());
        Assert.Equal("Bearer xoxb-test", request.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task LookupUserAsync_GetsSlackUserUsingEmailAlias()
    {
        var user = await _service.LookupUserAsync(User(email: "jdoe@myorg.com"));

        Assert.NotNull(user);
        Assert.EndsWith("email=john.doe%40myorg.com", _host.Http.Requests[0].Request.RequestUri!.ToString());
    }

    [Fact]
    public async Task LookupUserAsync_ReturnsNullIfNameAndEmailAreEmpty()
    {
        var user = await _service.LookupUserAsync(User(login: ""));

        Assert.Null(user);
        Assert.Empty(_host.Http.Requests);
    }

    [Fact]
    public void TranslateGitHubMarkdown_ConvertsHtmlTablesToText()
    {
        var result = _service.TranslateGitHubMarkdown("""
            <table>
              <tr>
                <td>row 1 cell 1</td>
                <td>row 1 cell 2</td>
              </tr>
              <tr>
                <td>row 2 cell 1</td>
                <td>row 2 cell 2</td>
              </tr>
            </table>
            """);

        Assert.Contains("row 1 cell 1  row 1 cell 2", result);
        Assert.Contains("row 2 cell 1  row 2 cell 2", result);
        Assert.DoesNotContain("<", result);
    }

    [Fact]
    public void TranslateGitHubMarkdown_ConvertsBrTagsToNewlines()
    {
        Assert.Equal("Line 1\nLine 2\nLine 3", _service.TranslateGitHubMarkdown("Line 1<br>Line 2<br/>Line 3"));
    }

    [Fact]
    public void TranslateGitHubMarkdown_RemovesHtmlComments()
    {
        var result = _service.TranslateGitHubMarkdown("Text before <!-- This is a comment --> text after");
        Assert.Equal("Text before  text after", result);
    }

    [Fact]
    public void TranslateGitHubMarkdown_StripsOtherHtmlTagsButPreservesContent()
    {
        var result = _service.TranslateGitHubMarkdown("<div>Content inside div</div><p>Paragraph content</p>");
        Assert.Equal("Content inside divParagraph content", result);
    }

    [Fact]
    public void TranslateGitHubMarkdown_HandlesEmptyMarkdown()
    {
        Assert.Equal(string.Empty, _service.TranslateGitHubMarkdown(""));
        Assert.Equal(string.Empty, _service.TranslateGitHubMarkdown(null));
    }

    [Fact]
    public void TranslateGitHubMarkdown_HandlesMixedHtmlAndMarkdown()
    {
        var result = _service.TranslateGitHubMarkdown("""
            # Heading

            Some **bold** text.

            <table>
              <tr>
                <td>Table content</td>
              </tr>
            </table>

            More text with <br>line break.
            """);

        Assert.StartsWith("*Heading*\n\nSome *bold* text.", result);
        Assert.Contains("Table content", result);
        Assert.Contains("More text with \nline break.", result);
    }

    private static User User(string login = "dselvam", string? name = null, string? email = null) =>
        GitHubModels.FromJson<User>(System.Text.Json.JsonSerializer.Serialize(new { login, name, email }));

    public void Dispose() => _host.Dispose();
}

using System.Net;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Models;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>
/// Contains tests that verify Slack service behavior.
/// </summary>
public class SlackServiceTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly SlackService _service;

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
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

    /// <summary>
    /// Verifies the expected behavior for this scenario: posts message to channel.
    /// </summary>
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

    /// <summary>
    /// Verifies the expected behavior for this scenario: does not post when disabled.
    /// </summary>
    [Fact]
    public async Task PostMessageAsync_DoesNotPostWhenDisabled()
    {
        _host.Context.Settings.Slack!.NotificationsEnabled = false;
        await _service.PostMessageAsync(SlackChannel.Deploy, "{}");

        Assert.Empty(_host.Http.Requests);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: gets Slack user by built email from name.
    /// </summary>
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

    /// <summary>
    /// Verifies the expected behavior for this scenario: gets Slack user using email alias.
    /// </summary>
    [Fact]
    public async Task LookupUserAsync_GetsSlackUserUsingEmailAlias()
    {
        var user = await _service.LookupUserAsync(User(email: "jdoe@myorg.com"));

        Assert.NotNull(user);
        Assert.EndsWith("email=john.doe%40myorg.com", _host.Http.Requests[0].Request.RequestUri!.ToString());
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: returns null if name and email are empty.
    /// </summary>
    [Fact]
    public async Task LookupUserAsync_ReturnsNullIfNameAndEmailAreEmpty()
    {
        var user = await _service.LookupUserAsync(User(login: ""));

        Assert.Null(user);
        Assert.Empty(_host.Http.Requests);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: converts HTML tables to text.
    /// </summary>
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

    /// <summary>
    /// Verifies the expected behavior for this scenario: converts br tags to newlines.
    /// </summary>
    [Fact]
    public void TranslateGitHubMarkdown_ConvertsBrTagsToNewlines()
    {
        Assert.Equal("Line 1\nLine 2\nLine 3", _service.TranslateGitHubMarkdown("Line 1<br>Line 2<br/>Line 3"));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: removes HTML comments.
    /// </summary>
    [Fact]
    public void TranslateGitHubMarkdown_RemovesHtmlComments()
    {
        var result = _service.TranslateGitHubMarkdown("Text before <!-- This is a comment --> text after");
        Assert.Equal("Text before  text after", result);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: strips other HTML tags but preserves content.
    /// </summary>
    [Fact]
    public void TranslateGitHubMarkdown_StripsOtherHtmlTagsButPreservesContent()
    {
        var result = _service.TranslateGitHubMarkdown("<div>Content inside div</div><p>Paragraph content</p>");
        Assert.Equal("Content inside divParagraph content", result);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: handles empty markdown.
    /// </summary>
    [Fact]
    public void TranslateGitHubMarkdown_HandlesEmptyMarkdown()
    {
        Assert.Equal(string.Empty, _service.TranslateGitHubMarkdown(""));
        Assert.Equal(string.Empty, _service.TranslateGitHubMarkdown(null));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: handles mixed HTML and markdown.
    /// </summary>
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

    /// <summary>
    /// Performs the user operation and returns its result.
    /// </summary>
    /// <param name="login">Text used as the login.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="email">Text used as the email.</param>
    /// <returns>The user produced by the operation.</returns>
    private static User User(string login = "dselvam", string? name = null, string? email = null) =>
        GitHubModels.FromJson<User>(System.Text.Json.JsonSerializer.Serialize(new { login, name, email }));

    /// <summary>
    /// Releases resources owned by this instance.
    /// </summary>
    public void Dispose() => _host.Dispose();
}

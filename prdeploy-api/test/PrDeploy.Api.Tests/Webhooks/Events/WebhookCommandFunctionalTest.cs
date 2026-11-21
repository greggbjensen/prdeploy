using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Auth;
using PrDeploy.Api.Webhooks.Events;
using PrDeploy.Api.Webhooks.Options;

namespace PrDeploy.Api.Tests.Webhooks.Events;

/// <summary>
/// Sends signed GitHub webhooks to <c>POST /webhooks</c> and verifies comment commands end to end through the queue,
/// worker, and dispatcher. Only the GitHub API is mocked.
/// </summary>
[Collection(ApiHostCollection.Name)]
public class WebhookCommandFunctionalTest : IClassFixture<WebhookCommandFunctionalTest.WebhookApiApplicationFactory>
{
    private const string Secret = "functional-test-secret";
    private const int PullNumber = 8;

    private readonly WebhookApiApplicationFactory _factory;
    private readonly Mock<IGitHubClient> _github;

    /// <summary>
    /// Resets the GitHub mock: pull request 8 is in this repository and holds the dev lock.
    /// </summary>
    /// <param name="factory">Shared API host configured to accept signed webhooks.</param>
    public WebhookCommandFunctionalTest(WebhookApiApplicationFactory factory)
    {
        _factory = factory;
        _github = factory.GitHub;
        _github.Reset();
        _github.Setup(g => g.Repository.Get(WebhookTestHost.Owner, WebhookTestHost.Repo))
            .ReturnsAsync(GitHubModels.FromJson<Octokit.Repository>("""{ "default_branch": "main" }"""));
        _github.Setup(g => g.Issue.Labels.GetAllForRepository(WebhookTestHost.Owner, WebhookTestHost.Repo))
            .ReturnsAsync([]);
        _github.Setup(g => g.PullRequest.Get(WebhookTestHost.Owner, WebhookTestHost.Repo, PullNumber))
            .ReturnsAsync(GitHubModels.PullRequest(PullNumber, labels: ["dev", "dev-lock"]));
    }

    /// <summary>
    /// Confirms a command from a user without write access is accepted by the endpoint but never acted on.
    /// </summary>
    [Fact]
    public async Task IssueComment_FromNonCollaborator_IsIgnored()
    {
        var checkedPermission = PermissionCheck("outsider",
            () => throw new NotFoundException("Not found", HttpStatusCode.NotFound));

        var status = await PostCommentAsync("outsider", "/free dev");

        Assert.Equal(HttpStatusCode.Accepted, status);
        await WaitAsync(checkedPermission.Task);
        _github.Verify(g => g.PullRequest.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _github.Verify(g => g.Issue.Labels.ReplaceAllForIssue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string[]>()), Times.Never);
    }

    /// <summary>
    /// Confirms a command from a user with write access runs.
    /// </summary>
    [Fact]
    public async Task IssueComment_FromWriter_RunsCommand()
    {
        var freed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        PermissionCheck("maintainer", () => GitHubModels.FromJson<CollaboratorPermissionResponse>(
            """{ "permission": "write", "role_name": "maintain" }"""));
        _github.Setup(g => g.Issue.Labels.ReplaceAllForIssue(WebhookTestHost.Owner, WebhookTestHost.Repo, PullNumber,
                It.IsAny<string[]>()))
            .Callback(() => freed.TrySetResult())
            .ReturnsAsync([]);

        var status = await PostCommentAsync("maintainer", "/free dev");

        Assert.Equal(HttpStatusCode.Accepted, status);
        await WaitAsync(freed.Task);
    }

    /// <summary>
    /// Confirms a webhook with a signature made from another secret is rejected before anything is queued.
    /// </summary>
    [Fact]
    public async Task IssueComment_WithInvalidSignature_IsUnauthorized()
    {
        using var client = _factory.CreateClient();
        using var request = CommentRequest("maintainer", "/deploy prod --force", "wrong-secret");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _github.Verify(g => g.Repository.Collaborator.ReviewPermission(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Sets up the permission GitHub reports for a user and signals when the check happens.
    /// </summary>
    /// <param name="login">User whose permission is checked.</param>
    /// <param name="result">Returns the response, or throws to simulate a GitHub error.</param>
    /// <returns>A source completed when the permission is checked.</returns>
    private TaskCompletionSource PermissionCheck(string login, Func<CollaboratorPermissionResponse> result)
    {
        var checkedPermission = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _github.Setup(g => g.Repository.Collaborator.ReviewPermission(WebhookTestHost.Owner, WebhookTestHost.Repo,
                login))
            .Returns(() =>
            {
                try
                {
                    return Task.FromResult(result());
                }
                finally
                {
                    checkedPermission.TrySetResult();
                }
            });
        return checkedPermission;
    }

    /// <summary>
    /// Posts a signed issue comment webhook.
    /// </summary>
    /// <param name="author">Comment author login.</param>
    /// <param name="body">Comment text.</param>
    /// <returns>The HTTP status the endpoint returned.</returns>
    private async Task<HttpStatusCode> PostCommentAsync(string author, string body)
    {
        using var client = _factory.CreateClient();
        using var request = CommentRequest(author, body, Secret);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    /// <summary>
    /// Builds an issue comment webhook request signed with the given secret.
    /// </summary>
    /// <param name="author">Comment author login.</param>
    /// <param name="body">Comment text.</param>
    /// <param name="secret">Secret used to sign the payload.</param>
    /// <returns>The request to send.</returns>
    private static HttpRequestMessage CommentRequest(string author, string body, string secret)
    {
        var payload = Encoding.UTF8.GetBytes($$"""
            {
              "action": "created",
              "comment": {
                "id": 99, "body": {{System.Text.Json.JsonSerializer.Serialize(body)}}, "user": { "login": "{{author}}" },
                "issue_url": "https://api.github.com/repos/{{WebhookTestHost.Owner}}/{{WebhookTestHost.Repo}}/issues/{{PullNumber}}"
              },
              "repository": { "name": "{{WebhookTestHost.Repo}}", "html_url": "{{WebhookTestHost.RepositoryUrl}}",
                              "owner": { "login": "{{WebhookTestHost.Owner}}" } },
              "installation": { "id": 42 }
            }
            """);
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks") { Content = new ByteArrayContent(payload) };
        request.Headers.Add(WebhookRequestHandler.EventHeader, "issue_comment");
        request.Headers.Add(WebhookRequestHandler.DeliveryHeader, Guid.NewGuid().ToString());
        request.Headers.Add(WebhookSignature.HeaderName, WebhookSignature.Compute(secret, payload));
        return request;
    }

    /// <summary>
    /// Waits for background webhook processing, failing after a timeout instead of hanging.
    /// </summary>
    /// <param name="task">Task completed by the expected GitHub call.</param>
    private static async Task WaitAsync(Task task)
    {
        var stopwatch = Stopwatch.StartNew();
        await task.WaitAsync(TimeSpan.FromSeconds(10));

        // Give the dispatcher a moment to make any further calls before the caller verifies they did not happen.
        await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(200, stopwatch.ElapsedMilliseconds + 50)));
    }

    /// <summary>
    /// API host that accepts webhooks signed with <see cref="Secret"/> and gives the GitHub App the mocked client.
    /// </summary>
    public sealed class WebhookApiApplicationFactory : DeployApiApplicationFactory
    {
        /// <summary>
        /// Gets the GitHub API mock used by the installation client.
        /// </summary>
        public Mock<IGitHubClient> GitHub { get; } = new() { DefaultValue = DefaultValue.Mock };

        /// <summary>
        /// Configures the webhook secret and replaces the installation client factory.
        /// </summary>
        /// <param name="builder">Host builder being configured.</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<GitHubAppOptions>(o => o.WebhookSecret = Secret);
                var clientFactory = new Mock<IGitHubAppClientFactory>();
                clientFactory.Setup(f => f.CreateInstallationClientAsync(42)).ReturnsAsync(() => GitHub.Object);
                services.RemoveAll<IGitHubAppClientFactory>();
                services.AddSingleton(clientFactory.Object);
            });
        }
    }
}

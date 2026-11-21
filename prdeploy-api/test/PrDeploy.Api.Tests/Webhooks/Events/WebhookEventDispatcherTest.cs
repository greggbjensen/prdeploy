using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks;
using PrDeploy.Api.Webhooks.Auth;
using PrDeploy.Api.Webhooks.Events;

namespace PrDeploy.Api.Tests.Webhooks.Events;

/// <summary>Contains tests that verify webhook event dispatcher behavior.</summary>
public class WebhookEventDispatcherTest
{
    private readonly Mock<IGitHubClient> _github = new() { DefaultValue = DefaultValue.Mock };
    private readonly InMemoryDeploymentStore _deploymentStore = new();
    private readonly WebhookEventDispatcher _dispatcher;

    /// <summary>Initializes the test fixture and prepares its dependencies.</summary>
    public WebhookEventDispatcherTest()
    {
        var clientFactory = new Mock<IGitHubAppClientFactory>();
        clientFactory.Setup(f => f.CreateInstallationClientAsync(42)).ReturnsAsync(_github.Object);

        var provider = new ServiceCollection()
            .AddLogging()
            .AddPrDeployWebhookServices()
            .AddSingleton<IDeploymentStore>(_deploymentStore)
            .BuildServiceProvider();

        _dispatcher = new WebhookEventDispatcher(provider.GetRequiredService<IServiceScopeFactory>(),
            clientFactory.Object, NullLogger<WebhookEventDispatcher>.Instance);
    }

    /// <summary>Verifies the expected behavior for this scenario: loads settings and runs comment command.</summary>
    [Fact]
    public async Task DispatchAsync_LoadsSettingsAndRunsCommentCommand()
    {
        await _deploymentStore.SetAsync(WebhookTestHost.Owner, "DEPLOY_SETTINGS", WebhookTestHost.CreateDefaultSettings());
        Mock.Get(_github.Object.Repository)
            .Setup(c => c.Get(WebhookTestHost.Owner, WebhookTestHost.Repo))
            .ReturnsAsync(GitHubModels.FromJson<Repository>("""{ "default_branch": "develop" }"""));
        Mock.Get(_github.Object.Issue.Labels)
            .Setup(c => c.GetAllForRepository(WebhookTestHost.Owner, WebhookTestHost.Repo))
            .ReturnsAsync([GitHubModels.FromJson<Label>("""{ "name": "dev", "color": "d4ac0d" }""")]);
        Mock.Get(_github.Object.PullRequest)
            .Setup(c => c.Get(WebhookTestHost.Owner, WebhookTestHost.Repo, 8))
            .ReturnsAsync(GitHubModels.PullRequest(8, labels: ["dev", "dev-lock"]));
        Mock.Get(_github.Object.Repository.Collaborator)
            .Setup(c => c.ReviewPermission(WebhookTestHost.Owner, WebhookTestHost.Repo, "octocat"))
            .ReturnsAsync(GitHubModels.FromJson<CollaboratorPermissionResponse>(
                """{ "permission": "write", "role_name": "write" }"""));

        await _dispatcher.DispatchAsync(Event("issue_comment", "created", """
            "comment": { "id": 99, "body": "/free dev", "user": { "login": "octocat" },
                         "issue_url": "https://api.github.com/repos/o/r/issues/8" }
            """));

        // The comment author's permission is checked before the command runs.
        Mock.Get(_github.Object.Repository.Collaborator).Verify(c => c.ReviewPermission(WebhookTestHost.Owner,
            WebhookTestHost.Repo, "octocat"));

        Mock.Get(_github.Object.Reaction.IssueComment).Verify(c => c.Create(WebhookTestHost.Owner,
            WebhookTestHost.Repo, 99, It.Is<NewReaction>(r => r.Content == ReactionType.Eyes)));
        Mock.Get(_github.Object.Issue.Labels).Verify(c => c.ReplaceAllForIssue(WebhookTestHost.Owner,
            WebhookTestHost.Repo, 8, new[] { "dev" }));

        // Environment labels are kept in sync with settings.
        Mock.Get(_github.Object.Issue.Labels).Verify(c => c.Update(WebhookTestHost.Owner, WebhookTestHost.Repo,
            It.IsAny<string>(), It.IsAny<LabelUpdate>()), Times.Never);
        Mock.Get(_github.Object.Issue.Labels).Verify(c => c.Create(WebhookTestHost.Owner, WebhookTestHost.Repo,
            It.Is<NewLabel>(l => l.Name == "dev-lock" && l.Color == "ededed")));
        Mock.Get(_github.Object.Issue.Labels).Verify(c => c.Create(WebhookTestHost.Owner, WebhookTestHost.Repo,
            It.Is<NewLabel>(l => l.Name == "stage" && l.Color == "2e86c1")));
    }

    /// <summary>Verifies the expected behavior for this scenario: deserializes pull request payload.</summary>
    [Fact]
    public async Task DispatchAsync_DeserializesPullRequestPayload()
    {
        var settings = WebhookTestHost.CreateDefaultSettings();
        settings.Jira!.AddIssuesEnabled = false;
        await _deploymentStore.SetAsync(WebhookTestHost.Owner, "DEPLOY_SETTINGS", settings);

        await _dispatcher.DispatchAsync(Event("pull_request", "closed", """
            "pull_request": { "number": 8, "merged": true, "head": { "ref": "b" }, "base": { "ref": "main" },
                              "user": { "login": "u" }, "labels": [{ "name": "stage" }, { "name": "stage-lock" }] }
            """));

        Mock.Get(_github.Object.Issue.Labels).Verify(c => c.ReplaceAllForIssue(WebhookTestHost.Owner,
            WebhookTestHost.Repo, 8, new[] { "stage" }));
    }

    /// <summary>Verifies the expected behavior for this scenario: logs and continues when flow fails.</summary>
    [Fact]
    public async Task DispatchAsync_LogsAndContinuesWhenFlowFails()
    {
        Mock.Get(_github.Object.Repository)
            .Setup(c => c.Get(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ApiException("Boom", System.Net.HttpStatusCode.InternalServerError));

        // Does not throw.
        await _dispatcher.DispatchAsync(Event("issue_comment", "created", """
            "comment": { "id": 99, "body": "/free dev", "issue_url": "https://api.github.com/repos/o/r/issues/8" }
            """));
    }

    /// <summary>Performs the event operation and returns its result.</summary>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="action">Text used as the action.</param>
    /// <param name="properties">Text used as the properties.</param>
    /// <returns>The webhook event produced by the operation.</returns>
    private static WebhookEvent Event(string name, string action, string properties)
    {
        using var document = JsonDocument.Parse($$"""
            {
              "action": "{{action}}",
              {{properties}},
              "repository": { "name": "{{WebhookTestHost.Repo}}", "html_url": "{{WebhookTestHost.RepositoryUrl}}",
                              "owner": { "login": "{{WebhookTestHost.Owner}}" } },
              "installation": { "id": 42 }
            }
            """);
        return new WebhookEvent(name, action, "delivery-1", document.RootElement.Clone());
    }
}

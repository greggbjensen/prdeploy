using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;
using PrDeploy.Api.Webhooks.Auth;
using PrDeploy.Api.Webhooks.Managers;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Webhooks.Events;

/// <summary>
/// Processes a webhook event in its own dependency injection scope, authenticated as the GitHub App installation.
/// </summary>
public class WebhookEventDispatcher
{
    // Deserializes GitHub payloads into the same models returned by the Octokit API clients.
    private static readonly SimpleJsonSerializer OctokitSerializer = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGitHubAppClientFactory _clientFactory;
    private readonly ILogger<WebhookEventDispatcher> _logger;

    public WebhookEventDispatcher(IServiceScopeFactory scopeFactory, IGitHubAppClientFactory clientFactory,
        ILogger<WebhookEventDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task DispatchAsync(WebhookEvent webhookEvent)
    {
        var payload = webhookEvent.Payload;
        var repository = payload.GetProperty("repository");
        var owner = repository.GetProperty("owner").GetProperty("login").GetString()!;
        var repo = repository.GetProperty("name").GetString()!;

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Owner"] = owner,
            ["Repo"] = repo,
            ["Event"] = $"{webhookEvent.Name}.{webhookEvent.Action}",
            ["DeliveryId"] = webhookEvent.DeliveryId
        });

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<WebhookContext>();
            context.Owner = owner;
            context.Repo = repo;
            context.RepositoryHtmlUrl = repository.GetProperty("html_url").GetString() ?? string.Empty;
            context.GitHub = await _clientFactory.CreateInstallationClientAsync(
                payload.GetProperty("installation").GetProperty("id").GetInt64());

            var repoSettings = await services.GetRequiredService<WebhookSettingsService>().GetAsync();
            context.Settings = repoSettings.Settings;
            context.DefaultBranch = repoSettings.DefaultBranch;

            await RouteAsync(webhookEvent, services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running flow for {Event}.{Action} ({DeliveryId}).", webhookEvent.Name,
                webhookEvent.Action, webhookEvent.DeliveryId);
        }
    }

    private async Task RouteAsync(WebhookEvent webhookEvent, IServiceProvider services)
    {
        var payload = webhookEvent.Payload;
        switch (webhookEvent.Name)
        {
            case WebhookEvent.PullRequest:
            {
                var pullRequest = Deserialize<PullRequest>(payload.GetProperty("pull_request"));
                using var _ = _logger.BeginScope(new Dictionary<string, object> { ["PullNumber"] = pullRequest.Number });
                _logger.LogDebug("Received a pull request {Action} for #{PullNumber}.", webhookEvent.Action,
                    pullRequest.Number);

                if (webhookEvent.Action == "closed")
                {
                    await services.GetRequiredService<StatusManager>().ProcessPullRequestClosedAsync(pullRequest);
                }
                else if (services.GetRequiredService<WebhookContext>().Settings.Jira?.AddIssuesEnabled != true)
                {
                    _logger.LogDebug("Add JIRA issues is disabled.");
                }
                else
                {
                    await services.GetRequiredService<JiraManager>().AddJiraIssuesToPullRequestAsync(pullRequest);
                }

                break;
            }

            case WebhookEvent.IssueComment:
            {
                var comment = payload.GetProperty("comment");
                var commentId = comment.GetProperty("id").GetInt64();
                var body = comment.GetProperty("body").GetString();
                _logger.LogDebug("Received a comment {Action} ({CommentId}):\n{Body}", webhookEvent.Action,
                    commentId, body);

                await services.GetRequiredService<CommentCommandManager>().ProcessCommentAsync(commentId, body,
                    comment.GetProperty("issue_url").GetString());
                break;
            }

            case WebhookEvent.WorkflowRun:
            {
                var run = Deserialize<WorkflowRun>(payload.GetProperty("workflow_run"));
                _logger.LogDebug("Received a workflow run {Action} for {Name}: {Title} ({RunId}) - {PullNumber}.",
                    webhookEvent.Action, run.Name, run.DisplayTitle, run.Id,
                    run.PullRequests?.FirstOrDefault()?.Number ?? 0);

                await services.GetRequiredService<StatusManager>().ProcessWorkflowRunAsync(run, webhookEvent.Action);
                break;
            }
        }
    }

    internal static T Deserialize<T>(JsonElement element) => OctokitSerializer.Deserialize<T>(element.GetRawText());
}

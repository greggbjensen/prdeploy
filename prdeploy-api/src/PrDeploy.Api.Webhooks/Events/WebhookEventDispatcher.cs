using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;
using PrDeploy.Api.Webhooks.Auth;
using PrDeploy.Api.Webhooks.Managers;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Webhooks.Events;

/// <summary>Processes a webhook event in its own dependency injection scope, authenticated as the GitHub App installation.</summary>
public class WebhookEventDispatcher
{
    // Deserializes GitHub payloads into the same models returned by the Octokit API clients.
    private static readonly SimpleJsonSerializer OctokitSerializer = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGitHubAppClientFactory _clientFactory;
    private readonly ILogger<WebhookEventDispatcher> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="scopeFactory">Object containing the scope factory data used by the operation.</param>
    /// <param name="clientFactory">Object containing the client factory data used by the operation.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public WebhookEventDispatcher(IServiceScopeFactory scopeFactory, IGitHubAppClientFactory clientFactory,
        ILogger<WebhookEventDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>Performs the dispatch operation.</summary>
    /// <param name="webhookEvent">Object containing the webhook event data used by the operation.</param>
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

            // Refresh the prdeploy check of every pull request whose deploy state the event changed.
            await services.GetRequiredService<DeployCheckService>().PublishTrackedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running flow for {Event}.{Action} ({DeliveryId}).", webhookEvent.Name,
                webhookEvent.Action, webhookEvent.DeliveryId);
        }
    }

    /// <summary>Performs the route operation.</summary>
    /// <param name="webhookEvent">Object containing the webhook event data used by the operation.</param>
    /// <param name="services">Services to include in the operation.</param>
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
                else if (webhookEvent.Action is "opened" or "edited")
                {
                    if (services.GetRequiredService<WebhookContext>().Settings.Jira?.AddIssuesEnabled != true)
                    {
                        _logger.LogDebug("Add JIRA issues is disabled.");
                    }
                    else
                    {
                        await services.GetRequiredService<JiraManager>().AddJiraIssuesToPullRequestAsync(pullRequest);
                    }
                }

                // New head commits need their own check run, so its buttons show in the merge box.
                if (webhookEvent.Action is "opened" or "reopened" or "synchronize")
                {
                    services.GetRequiredService<DeployCheckTracker>().Track(pullRequest.Number);
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

                var author = comment.TryGetProperty("user", out var user)
                             && user.TryGetProperty("login", out var login)
                    ? login.GetString()
                    : null;
                await services.GetRequiredService<CommentCommandManager>().ProcessCommentAsync(commentId, body,
                    comment.GetProperty("issue_url").GetString(), author);
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

            case WebhookEvent.CheckRun:
            {
                var checkRun = payload.GetProperty("check_run");
                var identifier = payload.GetProperty("requested_action").GetProperty("identifier").GetString();
                var pullNumbers = checkRun.TryGetProperty("pull_requests", out var pulls)
                                  && pulls.ValueKind == JsonValueKind.Array
                    ? pulls.EnumerateArray().Select(p => p.GetProperty("number").GetInt32()).ToList()
                    : [];
                _logger.LogDebug("Received check run action {Identifier} on {CheckName}.", identifier,
                    GetString(checkRun, "name"));

                await services.GetRequiredService<CheckRunActionManager>().ProcessRequestedActionAsync(
                    GetString(checkRun, "name"), identifier, GetString(checkRun, "external_id"), pullNumbers,
                    payload.TryGetProperty("sender", out var sender) ? GetString(sender, "login") : null);
                break;
            }

            case WebhookEvent.DeploymentProtectionRule:
            {
                var environment = GetString(payload, "environment");
                _logger.LogDebug("Received a deployment protection rule request for {Environment}.", environment);

                await services.GetRequiredService<DeploymentProtectionManager>().ProcessAsync(environment,
                    GetString(payload, "deployment_callback_url"));
                break;
            }
        }
    }

    /// <summary>Reads an optional string property from a payload object.</summary>
    /// <param name="element">Payload object to read.</param>
    /// <param name="name">Property name.</param>
    /// <returns>The value, or null when the property is missing or not a string.</returns>
    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>Performs the deserialize operation and returns its result.</summary>
    /// <param name="element">Object containing the element data used by the operation.</param>
    /// <typeparam name="T">Type of value consumed or produced by this operation.</typeparam>
    /// <returns>The t produced by the operation.</returns>
    internal static T Deserialize<T>(JsonElement element) => OctokitSerializer.Deserialize<T>(element.GetRawText());
}

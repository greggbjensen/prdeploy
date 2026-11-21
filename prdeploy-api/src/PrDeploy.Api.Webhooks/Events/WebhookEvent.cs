using System.Text.Json;

namespace PrDeploy.Api.Webhooks.Events;

/// <summary>A verified GitHub webhook delivery waiting to be processed.</summary>
/// <param name="Name">The X-GitHub-Event header, such as pull_request.</param>
/// <param name="Action">The action from the payload, such as opened.</param>
/// <param name="DeliveryId">The X-GitHub-Delivery header, used to trace the delivery in GitHub.</param>
/// <param name="Payload">The webhook payload.</param>
public record WebhookEvent(string Name, string Action, string DeliveryId, JsonElement Payload)
{
    public const string PullRequest = "pull_request";
    public const string IssueComment = "issue_comment";
    public const string WorkflowRun = "workflow_run";
    public const string Ping = "ping";

    private static readonly HashSet<string> HandledEvents =
    [
        $"{PullRequest}.opened",
        $"{PullRequest}.edited",
        $"{PullRequest}.closed",
        $"{IssueComment}.created",
        $"{WorkflowRun}.requested",
        $"{WorkflowRun}.completed"
    ];

    /// <summary>Gets the is handled.</summary>
    /// <value>Current is handled value.</value>
    public bool IsHandled => HandledEvents.Contains($"{Name}.{Action}");
}

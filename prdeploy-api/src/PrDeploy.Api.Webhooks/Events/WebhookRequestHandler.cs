using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Webhooks.Auth;
using PrDeploy.Api.Webhooks.Options;

namespace PrDeploy.Api.Webhooks.Events;

/// <summary>
/// Verifies GitHub webhook deliveries and queues the ones PR Deploy handles.
/// </summary>
public class WebhookRequestHandler
{
    public const string EventHeader = "X-GitHub-Event";
    public const string DeliveryHeader = "X-GitHub-Delivery";

    private readonly WebhookEventQueue _queue;
    private readonly GitHubAppOptions _options;
    private readonly ILogger<WebhookRequestHandler> _logger;

    public WebhookRequestHandler(WebhookEventQueue queue, IOptions<GitHubAppOptions> options,
        ILogger<WebhookRequestHandler> logger)
    {
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.WebhookSecret))
        {
            _logger.LogError("{Setting} is not configured, webhooks cannot be verified.",
                $"{GitHubAppOptions.SectionName}:{nameof(GitHubAppOptions.WebhookSecret)}");
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        using var bodyStream = new MemoryStream();
        await request.Body.CopyToAsync(bodyStream, cancellationToken);
        var body = bodyStream.ToArray();

        var eventName = request.Headers[EventHeader].ToString();
        var deliveryId = request.Headers[DeliveryHeader].ToString();
        if (!WebhookSignature.IsValid(_options.WebhookSecret, body, request.Headers[WebhookSignature.HeaderName]))
        {
            _logger.LogWarning("Rejected webhook {Event} ({DeliveryId}) with an invalid signature.", eventName,
                deliveryId);
            return Results.Unauthorized();
        }

        if (string.IsNullOrEmpty(eventName))
        {
            return Results.BadRequest($"Missing {EventHeader} header.");
        }

        if (eventName == WebhookEvent.Ping)
        {
            return Results.Ok();
        }

        JsonElement payload;
        try
        {
            using var document = JsonDocument.Parse(body);
            payload = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return Results.BadRequest("Payload is not valid JSON.");
        }

        var action = payload.TryGetProperty("action", out var actionElement) ? actionElement.GetString() ?? "" : "";
        var webhookEvent = new WebhookEvent(eventName, action, deliveryId, payload);
        if (!webhookEvent.IsHandled)
        {
            _logger.LogDebug("Ignoring webhook {Event}.{Action} ({DeliveryId}).", eventName, action, deliveryId);
            return Results.Ok();
        }

        if (!_queue.TryEnqueue(webhookEvent))
        {
            _logger.LogError("Webhook queue is full, rejected {Event}.{Action} ({DeliveryId}).", eventName, action,
                deliveryId);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        _logger.LogInformation("Queued webhook {Event}.{Action} ({DeliveryId}).", eventName, action, deliveryId);
        return Results.Accepted();
    }
}

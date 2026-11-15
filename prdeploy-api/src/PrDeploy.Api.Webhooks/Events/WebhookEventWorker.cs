using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Webhooks.Options;

namespace PrDeploy.Api.Webhooks.Events;

/// <summary>
/// Processes queued webhook events in the background with limited concurrency.
/// </summary>
public class WebhookEventWorker : BackgroundService
{
    private readonly WebhookEventQueue _queue;
    private readonly WebhookEventDispatcher _dispatcher;
    private readonly GitHubAppOptions _options;
    private readonly ILogger<WebhookEventWorker> _logger;

    public WebhookEventWorker(WebhookEventQueue queue, WebhookEventDispatcher dispatcher,
        IOptions<GitHubAppOptions> options, ILogger<WebhookEventWorker> logger)
    {
        _queue = queue;
        _dispatcher = dispatcher;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var concurrency = Math.Max(1, _options.MaxConcurrency);
        _logger.LogInformation("Processing webhook events with a concurrency of {Concurrency}.", concurrency);

        var consumers = Enumerable.Range(0, concurrency).Select(_ => ConsumeAsync(stoppingToken));
        return Task.WhenAll(consumers);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var webhookEvent in _queue.ReadAllAsync(stoppingToken))
            {
                // The dispatcher logs its own errors, this keeps the consumer alive for anything unexpected.
                try
                {
                    await _dispatcher.DispatchAsync(webhookEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error processing webhook {DeliveryId}.", webhookEvent.DeliveryId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }
}

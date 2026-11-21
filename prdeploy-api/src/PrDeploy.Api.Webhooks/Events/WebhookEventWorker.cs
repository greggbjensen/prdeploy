using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Webhooks.Options;

namespace PrDeploy.Api.Webhooks.Events;

/// <summary>Processes queued webhook events in the background with limited concurrency.</summary>
public class WebhookEventWorker : BackgroundService
{
    private readonly WebhookEventQueue _queue;
    private readonly WebhookEventDispatcher _dispatcher;
    private readonly GitHubAppOptions _options;
    private readonly ILogger<WebhookEventWorker> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="queue">Object containing the queue data used by the operation.</param>
    /// <param name="dispatcher">Object containing the dispatcher data used by the operation.</param>
    /// <param name="options">Object containing the options data used by the operation.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public WebhookEventWorker(WebhookEventQueue queue, WebhookEventDispatcher dispatcher,
        IOptions<GitHubAppOptions> options, ILogger<WebhookEventWorker> logger)
    {
        _queue = queue;
        _dispatcher = dispatcher;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Performs the execute operation.</summary>
    /// <param name="stoppingToken">Object containing the stopping token data used by the operation.</param>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var concurrency = Math.Max(1, _options.MaxConcurrency);
        _logger.LogInformation("Processing webhook events with a concurrency of {Concurrency}.", concurrency);

        var consumers = Enumerable.Range(0, concurrency).Select(_ => ConsumeAsync(stoppingToken));
        return Task.WhenAll(consumers);
    }

    /// <summary>Performs the consume operation.</summary>
    /// <param name="stoppingToken">Object containing the stopping token data used by the operation.</param>
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

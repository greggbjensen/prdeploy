using System.Threading.Channels;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Webhooks.Options;

namespace PrDeploy.Api.Webhooks.Events;

/// <summary>
/// In memory queue so webhook requests can be acknowledged before GitHub's 10 second delivery timeout.
/// </summary>
public class WebhookEventQueue
{
    private readonly Channel<WebhookEvent> _channel;

    /// <summary>
    /// Initializes a webhook Event Queue with the supplied dependencies.
    /// </summary>
    /// <param name="options">Value supplied for options.</param>
    public WebhookEventQueue(IOptions<GitHubAppOptions> options)
    {
        _channel = Channel.CreateBounded<WebhookEvent>(new BoundedChannelOptions(options.Value.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    /// <returns>False if the queue is full.</returns>
    public bool TryEnqueue(WebhookEvent webhookEvent) => _channel.Writer.TryWrite(webhookEvent);

    /// <summary>
    /// Performs the read All operation and returns its result.
    /// </summary>
    /// <param name="cancellationToken">Value supplied for cancellation Token.</param>
    /// <returns>The value produced by this operation.</returns>
    public IAsyncEnumerable<WebhookEvent> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

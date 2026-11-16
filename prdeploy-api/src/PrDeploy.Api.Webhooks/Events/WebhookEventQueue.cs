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
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="options">Object containing the options data used by the operation.</param>
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
    /// Performs the read all operation and returns its result.
    /// </summary>
    /// <param name="cancellationToken">Object containing the cancellation token data used by the operation.</param>
    /// <returns>The i async enumerable&lt;webhook event&gt; produced by the operation.</returns>
    public IAsyncEnumerable<WebhookEvent> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

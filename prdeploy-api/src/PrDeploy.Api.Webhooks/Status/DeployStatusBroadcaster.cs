using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace PrDeploy.Api.Webhooks.Status;

/// <summary>
/// Keeps the latest deployment workflow status per environment and fans changes out to connected dashboard streams.
/// State is held in memory, matching the single replica the webhook queue already requires.
/// </summary>
public class DeployStatusBroadcaster
{
    private readonly ConcurrentDictionary<string, DeployWorkflowStatus> _latest = new();
    private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers = new();

    /// <summary>
    /// Records a status and delivers it to every subscriber watching the same repository. A slow subscriber never
    /// blocks the publisher because its channel is bounded and drops its oldest pending update.
    /// </summary>
    /// <param name="status">Status to record and broadcast.</param>
    public void Publish(DeployWorkflowStatus status)
    {
        _latest[Key(status.Owner, status.Repo, status.Environment)] = status;
        foreach (var subscriber in _subscribers.Values)
        {
            if (subscriber.Matches(status.Owner, status.Repo))
            {
                subscriber.Channel.Writer.TryWrite(status);
            }
        }
    }

    /// <summary>
    /// Lists the latest known status for each environment of a repository.
    /// </summary>
    /// <param name="owner">Repository owner or organization.</param>
    /// <param name="repo">Repository name.</param>
    /// <returns>The current statuses, ordered by environment.</returns>
    public IReadOnlyList<DeployWorkflowStatus> GetSnapshot(string owner, string repo) => _latest.Values
        .Where(s => Matches(s, owner, repo))
        .OrderBy(s => s.Environment, StringComparer.OrdinalIgnoreCase)
        .ToList();

    /// <summary>
    /// Streams the current statuses of a repository followed by every later change until the caller cancels.
    /// Subscribing happens before the snapshot is read so no change between the two is lost.
    /// </summary>
    /// <param name="owner">Repository owner or organization.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="cancellationToken">Ends the stream, for example when the client disconnects.</param>
    /// <returns>An asynchronous sequence of statuses.</returns>
    public async IAsyncEnumerable<DeployWorkflowStatus> SubscribeAsync(string owner, string repo,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriber = new Subscriber(owner, repo);
        var id = Guid.NewGuid();
        _subscribers[id] = subscriber;
        try
        {
            foreach (var status in GetSnapshot(owner, repo))
            {
                yield return status;
            }

            await foreach (var status in subscriber.Channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return status;
            }
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Builds the dictionary key identifying one environment of one repository.
    /// </summary>
    private static string Key(string owner, string repo, string environment) =>
        $"{owner}/{repo}/{environment}".ToLowerInvariant();

    /// <summary>
    /// Determines whether a status belongs to a repository, ignoring case.
    /// </summary>
    private static bool Matches(DeployWorkflowStatus status, string owner, string repo) =>
        string.Equals(status.Owner, owner, StringComparison.OrdinalIgnoreCase)
        && string.Equals(status.Repo, repo, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A connected stream and the repository it watches.
    /// </summary>
    private sealed class Subscriber(string owner, string repo)
    {
        /// <summary>
        /// Gets the pending updates for this subscriber, keeping only the newest when it falls behind.
        /// </summary>
        public Channel<DeployWorkflowStatus> Channel { get; } = System.Threading.Channels.Channel
            .CreateBounded<DeployWorkflowStatus>(new BoundedChannelOptions(64)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            });

        /// <summary>
        /// Determines whether this subscriber watches the repository.
        /// </summary>
        public bool Matches(string statusOwner, string statusRepo) =>
            string.Equals(owner, statusOwner, StringComparison.OrdinalIgnoreCase)
            && string.Equals(repo, statusRepo, StringComparison.OrdinalIgnoreCase);
    }
}

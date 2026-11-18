using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PrDeploy.Api.Business.Stores.Interfaces;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Decorates a provider-specific <see cref="IDeploymentStore"/> with structured logging, so every storage
/// provider (local, Azure, AWS, GCP) and every consumer (API and webhooks) is logged the same way.
/// </summary>
/// <remarks>
/// Values are never logged because they may be marked secure. Only the owner, repo, value name, revision offset,
/// duration, and whether a value was found are recorded. Reads and writes are logged at Debug; failures are
/// logged at Error and rethrown unchanged.
/// </remarks>
public sealed class LoggingDeploymentStore : IDeploymentStore
{
    private readonly IDeploymentStore _inner;
    private readonly ILogger<LoggingDeploymentStore> _logger;

    /// <summary>
    /// Wraps a store with logging.
    /// </summary>
    /// <param name="inner">Provider-specific store that performs the actual reads and writes.</param>
    /// <param name="logger">Logger used to report storage activity and failures.</param>
    public LoggingDeploymentStore(IDeploymentStore inner, ILogger<LoggingDeploymentStore> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string owner, string repo, string name) =>
        ReadAsync(() => _inner.GetAsync<T>(owner, repo, name), owner, repo, name, 0);

    /// <inheritdoc />
    public Task<T> GetHistoricalAsync<T>(string owner, string repo, string name, int historical) =>
        ReadAsync(() => _inner.GetHistoricalAsync<T>(owner, repo, name, historical), owner, repo, name, historical);

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string owner, string name) =>
        ReadAsync(() => _inner.GetAsync<T>(owner, name), owner, null, name, 0);

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string name) =>
        ReadAsync(() => _inner.GetAsync<T>(name), null, null, name, 0);

    /// <inheritdoc />
    public Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false) =>
        WriteAsync(() => _inner.SetAsync(owner, repo, name, value, isSecure), owner, repo, name, isSecure);

    /// <inheritdoc />
    public Task SetAsync<T>(string owner, string name, T value, bool isSecure = false) =>
        WriteAsync(() => _inner.SetAsync(owner, name, value, isSecure), owner, null, name, isSecure);

    /// <inheritdoc />
    public Task SetAsync<T>(string name, T value, bool isSecure = false) =>
        WriteAsync(() => _inner.SetAsync(name, value, isSecure), null, null, name, isSecure);

    /// <summary>
    /// Runs a read against the inner store and logs its outcome without ever logging the value.
    /// </summary>
    /// <param name="read">Read to run against the inner store.</param>
    /// <param name="owner">Repository owner, or null for global values.</param>
    /// <param name="repo">Repository name, or null for owner or global values.</param>
    /// <param name="name">Deployment-value name.</param>
    /// <param name="historical">Revision offset requested, where 0 is the current value.</param>
    /// <typeparam name="T">Type of value read.</typeparam>
    /// <returns>The value returned by the inner store.</returns>
    private async Task<T> ReadAsync<T>(Func<Task<T>> read, string? owner, string? repo, string name, int historical)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var value = await read().ConfigureAwait(false);
            _logger.LogDebug(
                "Read deployment value {Name} for {Owner}/{Repo} (revision offset {Historical}, found {Found}) in {ElapsedMs}ms.",
                name, owner, repo, historical, !EqualityComparer<T>.Default.Equals(value, default!),
                stopwatch.ElapsedMilliseconds);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to read deployment value {Name} for {Owner}/{Repo} (revision offset {Historical}) after {ElapsedMs}ms.",
                name, owner, repo, historical, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Runs a write against the inner store and logs its outcome without ever logging the value.
    /// </summary>
    /// <param name="write">Write to run against the inner store.</param>
    /// <param name="owner">Repository owner, or null for global values.</param>
    /// <param name="repo">Repository name, or null for owner or global values.</param>
    /// <param name="name">Deployment-value name.</param>
    /// <param name="isSecure">Whether the caller marked the value as sensitive.</param>
    private async Task WriteAsync(Func<Task> write, string? owner, string? repo, string name, bool isSecure)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await write().ConfigureAwait(false);
            _logger.LogDebug(
                "Stored deployment value {Name} for {Owner}/{Repo} (secure {IsSecure}) in {ElapsedMs}ms.",
                name, owner, repo, isSecure, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to store deployment value {Name} for {Owner}/{Repo} after {ElapsedMs}ms.",
                name, owner, repo, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

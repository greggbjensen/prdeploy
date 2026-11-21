using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Stores.Interfaces;

namespace PrDeploy.Api.Business.Stores;

/// <summary>Stores serialized deployment values and their revisions in the local BLite database.</summary>
public sealed class BliteDeploymentStore : IDeploymentStore
{
    private readonly BliteStorage _storage;
    private readonly DeploymentStoreOptions _storageOptions;

    /// <summary>Creates a store backed by the shared BLite database.</summary>
    public BliteDeploymentStore(BliteStorage storage, IOptions<DeploymentStoreOptions> storageOptions)
    {
        _storage = storage;
        _storageOptions = storageOptions.Value;
    }

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string owner, string repo, string name) =>
        GetDeploymentValue<T>(RepositoryPath(owner, repo, name), 0);

    /// <inheritdoc />
    public Task<T> GetHistoricalAsync<T>(string owner, string repo, string name, int historical) =>
        GetDeploymentValue<T>(RepositoryPath(owner, repo, name), Math.Max(historical, 0));

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string owner, string name) =>
        GetDeploymentValue<T>($"{_storageOptions.DeploymentValuePathPrefix}/{owner}/{name.ToUpperInvariant()}", 0);

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string name) =>
        GetDeploymentValue<T>($"{_storageOptions.DeploymentValuePathPrefix}/{name.ToUpperInvariant()}", 0);

    /// <inheritdoc />
    public Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false) =>
        SetDeploymentValue(RepositoryPath(owner, repo, name), value, isSecure);

    /// <inheritdoc />
    public Task SetAsync<T>(string owner, string name, T value, bool isSecure = false) =>
        SetDeploymentValue($"{_storageOptions.DeploymentValuePathPrefix}/{owner}/{name.ToUpperInvariant()}", value, isSecure);

    /// <inheritdoc />
    public Task SetAsync<T>(string name, T value, bool isSecure = false) =>
        SetDeploymentValue($"{_storageOptions.DeploymentValuePathPrefix}/{name.ToUpperInvariant()}", value, isSecure);

    /// <summary>Performs the repository path operation and returns its result.</summary>
    private string RepositoryPath(string owner, string repo, string name) =>
        $"{_storageOptions.DeploymentValuePathPrefix}/{owner}/{repo}/{name.ToUpperInvariant()}";

    /// <summary>Reads the requested historical revision from the local deployment-value collection.</summary>
    private Task<T> GetDeploymentValue<T>(string path, int historical) =>
        _storage.ExecuteAsync(async context =>
        {
            var matches = new List<DeploymentRevision>();
            await foreach (var revision in context.DeploymentValues.FindAsync(value => value.Path == path))
            {
                matches.Add(revision);
            }

            var selectedRevision = matches
                .OrderByDescending(value => value.Revision)
                .Skip(historical)
                .FirstOrDefault();
            return selectedRevision is null
                ? default!
                : DeploymentValueSerialization.Deserialize<T>(selectedRevision.Value);
        });

    /// <summary>Appends a serialized revision to the local deployment-value collection.</summary>
    private Task SetDeploymentValue<T>(string path, T value, bool isSecure)
    {
        var serializedValue = DeploymentValueSerialization.Serialize(value);
        return _storage.ExecuteAsync(async context =>
        {
            var latestRevision = 0L;
            await foreach (var revision in context.DeploymentValues.FindAsync(item => item.Path == path))
            {
                latestRevision = Math.Max(latestRevision, revision.Revision);
            }

            await context.DeploymentValues.InsertAsync(new DeploymentRevision
            {
                Id = Guid.NewGuid().ToString("N"),
                Path = path,
                Revision = latestRevision + 1,
                Value = serializedValue,
                IsSecure = isSecure
            }).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        });
    }
}

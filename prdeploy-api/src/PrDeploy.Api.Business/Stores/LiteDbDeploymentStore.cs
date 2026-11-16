using LiteDB;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Stores.Interfaces;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Stores serialized deployment values and their revisions in the local LiteDB database.
/// </summary>
public sealed class LiteDbDeploymentStore : IDeploymentStore
{
    private readonly LiteDbStorage _storage;
    private readonly DeploymentStoreOptions _storageOptions;

    /// <summary>
    /// Creates a store backed by the shared LiteDB database.
    /// </summary>
    public LiteDbDeploymentStore(LiteDbStorage storage, IOptions<DeploymentStoreOptions> storageOptions)
    {
        _storage = storage;
        _storageOptions = storageOptions.Value;
    }

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string owner, string repo, string name) =>
        Task.FromResult(GetDeploymentValue<T>(RepositoryPath(owner, repo, name), 0));

    /// <inheritdoc />
    public Task<T> GetHistoricalAsync<T>(string owner, string repo, string name, int historical) =>
        Task.FromResult(GetDeploymentValue<T>(RepositoryPath(owner, repo, name), Math.Max(historical, 0)));

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string owner, string name) =>
        Task.FromResult(GetDeploymentValue<T>($"{_storageOptions.DeploymentValuePathPrefix}/{owner}/{name.ToUpperInvariant()}", 0));

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string name) =>
        Task.FromResult(GetDeploymentValue<T>($"{_storageOptions.DeploymentValuePathPrefix}/{name.ToUpperInvariant()}", 0));

    /// <inheritdoc />
    public Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false)
    {
        SetDeploymentValue(RepositoryPath(owner, repo, name), value, isSecure);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string owner, string name, T value, bool isSecure = false)
    {
        SetDeploymentValue($"{_storageOptions.DeploymentValuePathPrefix}/{owner}/{name.ToUpperInvariant()}", value, isSecure);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string name, T value, bool isSecure = false)
    {
        SetDeploymentValue($"{_storageOptions.DeploymentValuePathPrefix}/{name.ToUpperInvariant()}", value, isSecure);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Builds the normalized path used to isolate a value within a repository.
    /// </summary>
    private string RepositoryPath(string owner, string repo, string name) =>
        $"{_storageOptions.DeploymentValuePathPrefix}/{owner}/{repo}/{name.ToUpperInvariant()}";

    /// <summary>
    /// Reads the requested historical revision from the local deployment-value collection.
    /// </summary>
    private T GetDeploymentValue<T>(string path, int historical)
    {
        lock (_storage.Gate)
        {
            var values = _storage.GetCollection<DeploymentRevision>("DeploymentValues");
            var revision = values.Find(x => x.Path == path)
                .OrderByDescending(x => x.Revision)
                .Skip(historical)
                .FirstOrDefault();
            return revision is null ? default! : DeploymentValueSerialization.Deserialize<T>(revision.Value);
        }
    }

    /// <summary>
    /// Appends a serialized revision to the local deployment-value collection.
    /// </summary>
    private void SetDeploymentValue<T>(string path, T value, bool isSecure)
    {
        var serializedValue = DeploymentValueSerialization.Serialize(value);
        lock (_storage.Gate)
        {
            var values = _storage.GetCollection<DeploymentRevision>("DeploymentValues");
            values.EnsureIndex(x => new { x.Path, x.Revision }, unique: true);
            var latestRevision = values.Find(x => x.Path == path)
                .Select(x => x.Revision)
                .DefaultIfEmpty(0)
                .Max();
            values.Insert(new DeploymentRevision
            {
                Id = ObjectId.NewObjectId(),
                Path = path,
                Revision = latestRevision + 1,
                Value = serializedValue,
                IsSecure = isSecure
            });
        }
    }

    /// <summary>
    /// Represents one immutable revision of a deployment value.
    /// </summary>
    private sealed class DeploymentRevision
    {
        /// <summary>
        /// Gets or sets the LiteDB-generated document identifier.
        /// </summary>
        [BsonId]
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        /// <summary>
        /// Gets or sets the normalized deployment-value path.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the monotonically increasing revision for this path.
        /// </summary>
        public long Revision { get; set; }

        /// <summary>
        /// Gets or sets the serialized value payload.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the caller marked this revision as secure.
        /// </summary>
        public bool IsSecure { get; set; }
    }
}

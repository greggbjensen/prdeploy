using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Stores.Interfaces;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Stores deployment values in the configured Google Cloud Firestore collection.
/// </summary>
public sealed class FirestoreDeploymentStore(
    FirestoreDocumentStorage storage,
    IOptions<DeploymentStoreOptions> storageOptions) : IDeploymentStore
{
    /// <inheritdoc />
    public Task<T> GetAsync<T>(string owner, string repo, string name) =>
        GetDeploymentValueAsync<T>(RepositoryPath(owner, repo, name), 0);

    /// <inheritdoc />
    public Task<T> GetHistoricalAsync<T>(string owner, string repo, string name, int historical) =>
        GetDeploymentValueAsync<T>(RepositoryPath(owner, repo, name), Math.Max(historical, 0));

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string owner, string name) =>
        GetDeploymentValueAsync<T>($"{storageOptions.Value.DeploymentValuePathPrefix}/{owner}/{name.ToUpperInvariant()}", 0);

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string name) =>
        GetDeploymentValueAsync<T>($"{storageOptions.Value.DeploymentValuePathPrefix}/{name.ToUpperInvariant()}", 0);

    /// <inheritdoc />
    public Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false) =>
        SetDeploymentValueAsync(RepositoryPath(owner, repo, name), value, isSecure);

    /// <inheritdoc />
    public Task SetAsync<T>(string owner, string name, T value, bool isSecure = false) =>
        SetDeploymentValueAsync($"{storageOptions.Value.DeploymentValuePathPrefix}/{owner}/{name.ToUpperInvariant()}", value, isSecure);

    /// <inheritdoc />
    public Task SetAsync<T>(string name, T value, bool isSecure = false) =>
        SetDeploymentValueAsync($"{storageOptions.Value.DeploymentValuePathPrefix}/{name.ToUpperInvariant()}", value, isSecure);

    /// <summary>
    /// Builds the normalized path used to isolate a value within a repository.
    /// </summary>
    private string RepositoryPath(string owner, string repo, string name) =>
        $"{storageOptions.Value.DeploymentValuePathPrefix}/{owner}/{repo}/{name.ToUpperInvariant()}";

    /// <summary>
    /// Reads the requested historical revision and deserializes its value payload.
    /// </summary>
    private async Task<T> GetDeploymentValueAsync<T>(string path, int historical)
    {
        var snapshot = await storage.GetDeploymentValueAsync(path, historical).ConfigureAwait(false);
        return snapshot is null
            ? default!
            : DeploymentValueSerialization.Deserialize<T>(snapshot.GetValue<string>("value"));
    }

    /// <summary>
    /// Serializes and appends a deployment-value revision.
    /// </summary>
    private Task SetDeploymentValueAsync<T>(string path, T value, bool isSecure) =>
        storage.SetDeploymentValueAsync(path, DeploymentValueSerialization.Serialize(value), isSecure);
}

using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Stores.Interfaces;

namespace PrDeploy.Api.Business.Stores;

/// <summary>Stores deployment values in the configured Azure Cosmos DB container.</summary>
/// <param name="storage">Cosmos DB document adapter used to read and append deployment-value revisions.</param>
/// <param name="storageOptions">Options containing the path prefix shared by deployment-value keys.</param>
public sealed class CosmosDeploymentStore(
    CosmosDocumentStorage storage,
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

    /// <summary>Performs the repository path operation and returns its result.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <returns>Text produced or selected by the operation.</returns>
    private string RepositoryPath(string owner, string repo, string name) =>
        $"{storageOptions.Value.DeploymentValuePathPrefix}/{owner}/{repo}/{name.ToUpperInvariant()}";

    /// <summary>Reads the requested historical revision and deserializes its value payload.</summary>
    private async Task<T> GetDeploymentValueAsync<T>(string path, int historical)
    {
        var document = await storage.GetDeploymentValueAsync(path, historical).ConfigureAwait(false);
        return document is null ? default! : DeploymentValueSerialization.Deserialize<T>(document.Value);
    }

    /// <summary>Serializes and appends a deployment-value revision.</summary>
    private Task SetDeploymentValueAsync<T>(string path, T value, bool isSecure) =>
        storage.SetDeploymentValueAsync(path, DeploymentValueSerialization.Serialize(value), isSecure);
}

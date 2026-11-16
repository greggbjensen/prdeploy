using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Provides document operations for deployment values and Data Protection keys in Cosmos DB.
/// </summary>
public sealed class CosmosDocumentStorage : IDisposable
{
    private readonly CosmosClient _client;
    private readonly Lazy<Task<Container>> _container;

    /// <summary>
    /// Creates a document-storage adapter for the specified Cosmos DB database and container.
    /// </summary>
    /// <param name="client">The Cosmos DB client used to access the database.</param>
    /// <param name="databaseName">The database name to create or open.</param>
    /// <param name="containerName">The container name to create or open.</param>
    public CosmosDocumentStorage(CosmosClient client, string databaseName, string containerName)
    {
        _client = client;
        DatabaseName = databaseName;
        ContainerName = containerName;
        _container = new Lazy<Task<Container>>(InitializeContainerAsync);
    }

    /// <summary>
    /// Gets the Cosmos DB database used by this storage instance.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// Gets the Cosmos DB container used by this storage instance.
    /// </summary>
    public string ContainerName { get; }

    /// <summary>
    /// Retrieves a deployment value revision by its storage path.
    /// </summary>
    public async Task<StorageDocument?> GetDeploymentValueAsync(string path, int historical)
    {
        var container = await _container.Value.ConfigureAwait(false);
        var query = new QueryDefinition("""
            SELECT * FROM c
            WHERE c.documentType = "deploymentValue" AND c.path = @path
            ORDER BY c.revision DESC, c.id DESC
            """).WithParameter("@path", path);
        using var iterator = container.GetItemQueryIterator<StorageDocument>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(path) });

        var remaining = Math.Max(historical, 0);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync().ConfigureAwait(false);
            foreach (var item in response)
            {
                if (remaining-- == 0)
                {
                    return item;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Appends a deployment value revision to its path partition.
    /// </summary>
    public async Task SetDeploymentValueAsync(string path, string value, bool isSecure)
    {
        var container = await _container.Value.ConfigureAwait(false);
        await container.CreateItemAsync(new StorageDocument
        {
            Id = $"deployment-value:{Guid.NewGuid():N}",
            PartitionKey = path,
            DocumentType = "deploymentValue",
            Path = path,
            Value = value,
            IsSecure = isSecure,
            Revision = DateTime.UtcNow.Ticks
        }, new PartitionKey(path)).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads all persisted Data Protection key documents.
    /// </summary>
    public async Task<IReadOnlyList<StorageDocument>> GetDataProtectionKeysAsync()
    {
        var container = await _container.Value.ConfigureAwait(false);
        var query = new QueryDefinition("""
            SELECT * FROM c WHERE c.documentType = "dataProtectionKey"
            ORDER BY c.id
            """);
        using var iterator = container.GetItemQueryIterator<StorageDocument>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(DataProtectionPartition)
            });

        var documents = new List<StorageDocument>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync().ConfigureAwait(false);
            documents.AddRange(response);
        }

        return documents;
    }

    /// <summary>
    /// Adds or replaces a Data Protection key document by its friendly name.
    /// </summary>
    public async Task StoreDataProtectionKeyAsync(string friendlyName, string xml)
    {
        var container = await _container.Value.ConfigureAwait(false);
        var id = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(friendlyName))).ToLowerInvariant();
        await container.UpsertItemAsync(new StorageDocument
        {
            Id = $"key:{id}",
            PartitionKey = DataProtectionPartition,
            DocumentType = "dataProtectionKey",
            FriendlyName = friendlyName,
            Xml = xml
        }, new PartitionKey(DataProtectionPartition)).ConfigureAwait(false);
    }

    /// <summary>
    /// Partition used to keep Data Protection key documents separate from deployment values.
    /// </summary>
    public const string DataProtectionPartition = "data-protection";

    /// <summary>
    /// Releases the Cosmos client owned by this storage instance.
    /// </summary>
    public void Dispose() => _client.Dispose();

    /// <summary>
    /// Creates the configured database and container when needed, then returns the container handle.
    /// </summary>
    private async Task<Container> InitializeContainerAsync()
    {
        var databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(DatabaseName)
            .ConfigureAwait(false);
        var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
            ContainerName,
            "/partitionKey").ConfigureAwait(false);
        return containerResponse.Container;
    }
}

/// <summary>
/// Cosmos DB document shape shared by deployment value revisions and Data Protection keys.
/// </summary>
public sealed class StorageDocument
{
    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document partition key.
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document kind.
    /// </summary>
    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment-value path.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized deployment value.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the deployment value was marked secure by its caller.
    /// </summary>
    [JsonPropertyName("isSecure")]
    public bool IsSecure { get; set; }

    /// <summary>
    /// Gets or sets the revision sequence.
    /// </summary>
    [JsonPropertyName("revision")]
    public long Revision { get; set; }

    /// <summary>
    /// Gets or sets the Data Protection key friendly name.
    /// </summary>
    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Data Protection key XML.
    /// </summary>
    [JsonPropertyName("xml")]
    public string Xml { get; set; } = string.Empty;
}

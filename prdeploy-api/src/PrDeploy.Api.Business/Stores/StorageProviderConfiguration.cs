using Microsoft.Extensions.Configuration;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Identifies the selected deployment-value document database provider.
/// </summary>
public enum StorageProvider
{
    /// <summary>
    /// Stores documents locally in LiteDB without cloud credentials.
    /// </summary>
    Local,

    /// <summary>
    /// Stores documents in Azure Cosmos DB.
    /// </summary>
    Azure,

    /// <summary>
    /// Stores documents in Amazon DynamoDB.
    /// </summary>
    AWS,

    /// <summary>
    /// Stores documents in Google Cloud Firestore.
    /// </summary>
    GCP
}

/// <summary>
/// Resolves provider selection and provider-specific settings from the Storage configuration section.
/// </summary>
public static class StorageProviderConfiguration
{
    /// <summary>
    /// Gets the selected provider, defaulting to Local when no provider is configured.
    /// </summary>
    public static StorageProvider GetProvider(IConfiguration configuration)
    {
        var provider = configuration["Storage:Provider"];
        if (string.IsNullOrWhiteSpace(provider) ||
            provider.Equals("Local", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("LiteDB", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            return StorageProvider.Local;
        }

        if (provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
        {
            return StorageProvider.Azure;
        }

        if (provider.Equals("AWS", StringComparison.OrdinalIgnoreCase))
        {
            return StorageProvider.AWS;
        }

        if (provider.Equals("GCP", StringComparison.OrdinalIgnoreCase))
        {
            return StorageProvider.GCP;
        }

        throw new InvalidOperationException(
            $"Unsupported storage provider '{provider}'. Supported values are 'Local', 'Azure', 'AWS', and 'GCP'.");
    }

    /// <summary>
    /// Gets the absolute path for the local LiteDB database.
    /// </summary>
    public static string GetLocalDatabasePath(IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:Local:DatabasePath"]
            ?? configuration["Storage:DatabasePath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException(
                "A local data directory is unavailable. Configure 'Storage:Local:DatabasePath'.");
        }

        return Path.Combine(localApplicationData, "prdeploy", "deployment-storage.db");
    }

    /// <summary>
    /// Gets the configured DynamoDB table name or its default.
    /// </summary>
    public static string GetAwsTableName(IConfiguration configuration) =>
        GetRequiredOrDefault(configuration["Storage:AWS:TableName"], "PrDeployStorage");

    /// <summary>
    /// Gets the configured Cosmos DB database name or its default.
    /// </summary>
    public static string GetCosmosDatabaseName(IConfiguration configuration) =>
        GetRequiredOrDefault(configuration["Storage:Azure:DatabaseName"], "prdeploy");

    /// <summary>
    /// Gets the configured Cosmos DB container name or its default.
    /// </summary>
    public static string GetCosmosContainerName(IConfiguration configuration) =>
        GetRequiredOrDefault(configuration["Storage:Azure:ContainerName"], "storage");

    /// <summary>
    /// Gets the configured Firestore collection name or its default.
    /// </summary>
    public static string GetFirestoreCollectionName(IConfiguration configuration) =>
        GetRequiredOrDefault(configuration["Storage:GCP:CollectionName"], "prdeploy_storage");

    /// <summary>
    /// Returns a nonblank configured name or its provider-specific default.
    /// </summary>
    private static string GetRequiredOrDefault(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
}

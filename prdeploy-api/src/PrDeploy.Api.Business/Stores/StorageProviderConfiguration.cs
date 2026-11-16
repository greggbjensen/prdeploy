using Microsoft.Extensions.Configuration;

namespace PrDeploy.Api.Business.Stores;

/// <summary>
/// Identifies the selected deployment-value document database provider.
/// </summary>
public enum StorageProvider
{
    /// <summary>
    /// Stores documents locally in BLite without cloud credentials.
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
    /// Selects the configured deployment-value provider, defaulting to local storage when none is specified.
    /// </summary>
    /// <param name="configuration">Configuration containing the selected provider and provider-specific settings.</param>
    /// <returns>The storage provider selected by the application configuration.</returns>
    public static StorageProvider GetProvider(IConfiguration configuration)
    {
        var provider = configuration["Storage:Provider"];
        if (string.IsNullOrWhiteSpace(provider) ||
            provider.Equals("Local", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("LiteDB", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("BLite", StringComparison.OrdinalIgnoreCase))
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
    /// Resolves the local database path from configuration or the per-user application data directory.
    /// </summary>
    /// <param name="configuration">Configuration containing the optional local database path.</param>
    /// <returns>An absolute path to the local deployment-value database.</returns>
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

        return Path.Combine(localApplicationData, "prdeploy", "deployment-storage.blite");
    }

    /// <summary>
    /// Retrieves the AWS table name.
    /// </summary>
    /// <param name="configuration">Object containing the configuration data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public static string GetAwsTableName(IConfiguration configuration) =>
        GetRequiredOrDefault(configuration["Storage:AWS:TableName"], "PrDeployStorage");

    /// <summary>
    /// Retrieves the cosmos database name.
    /// </summary>
    /// <param name="configuration">Object containing the configuration data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public static string GetCosmosDatabaseName(IConfiguration configuration) =>
        GetRequiredOrDefault(configuration["Storage:Azure:DatabaseName"], "prdeploy");

    /// <summary>
    /// Retrieves the cosmos container name.
    /// </summary>
    /// <param name="configuration">Object containing the configuration data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public static string GetCosmosContainerName(IConfiguration configuration) =>
        GetRequiredOrDefault(configuration["Storage:Azure:ContainerName"], "storage");

    /// <summary>
    /// Retrieves the firestore collection name.
    /// </summary>
    /// <param name="configuration">Object containing the configuration data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public static string GetFirestoreCollectionName(IConfiguration configuration) =>
        GetRequiredOrDefault(configuration["Storage:GCP:CollectionName"], "prdeploy_storage");

    /// <summary>
    /// Returns a nonblank configured name or its provider-specific default.
    /// </summary>
    private static string GetRequiredOrDefault(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
}

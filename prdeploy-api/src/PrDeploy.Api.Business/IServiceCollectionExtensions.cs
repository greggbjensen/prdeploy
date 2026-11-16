using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleSystemsManagement;
using Google.Cloud.Firestore;
using LiteDB;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Auth;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Clients;
using PrDeploy.Api.Business.Clients.Interfaces;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Security;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Business.Stores;
using PrDeploy.Api.Business.Stores.Interfaces;
using RestSharp;

namespace PrDeploy.Api.Business;

/// <summary>
/// Adds the business services and document-storage provider used by PrDeploy.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registers PrDeploy business services and the configured deployment-value store.
    /// </summary>
    public static IServiceCollection AddPrDeployApiBusiness(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .Configure<GitHubAuthOptions>(configuration.GetSection("GitHubAuth"))
            .Configure<JwtOptions>(configuration.GetSection("Jwt"))
            .AddScoped<IRestClientInstance<GitHubAuthOptions>>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<GitHubAuthOptions>>();
                return new RestClientInstance<GitHubAuthOptions>
                {
                    Options = options.Value,
                    RestClient = new RestClient(new RestClientOptions(options.Value.Authority))
                };
            })
            .AddMemoryCache()
            .AddScoped<IDeployQueueService, DeployQueueService>()
            .AddScoped<IDeployEnvironmentService, DeployEnvironmentService>()
            .AddScoped<IPullRequestService, PullRequestService>()
            .AddScoped<IDeploySettingsService, DeploySettingsService>()
            .AddScoped<IOwnerRepoService, OwnerRepoService>()
            .AddScoped<IGitHubAuthClient, GitHubAuthClient>()
            .AddScoped<IGitHubSecurity, GitHubSecurity>()
            .AddScoped<ICipherService, CipherService>();

        services.AddPrDeployStorage(configuration);
        return services;
    }

    /// <summary>
    /// Registers the selected document-storage backend and its Data Protection key repository.
    /// </summary>
    /// <remarks>
    /// The default Local provider uses LiteDB and does not register or resolve cloud SDK clients
    /// or credential providers.
    /// </remarks>
    public static IServiceCollection AddPrDeployStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DeploymentStoreOptions>(configuration.GetSection("Storage"));
        services.PostConfigure<DeploymentStoreOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(configuration["Storage:DeploymentValuePathPrefix"]))
            {
                options.DeploymentValuePathPrefix = configuration["Aws:SecretPathPrefix"]
                    ?? options.DeploymentValuePathPrefix;
            }
        });

        switch (StorageProviderConfiguration.GetProvider(configuration))
        {
            case StorageProvider.Local:
                var databasePath = StorageProviderConfiguration.GetLocalDatabasePath(configuration);
                var localPassword = configuration["Storage:Local:Password"];
                services.TryAddSingleton(_ => new LiteDbStorage(databasePath, localPassword));
                services.TryAddSingleton<IXmlRepository, LiteDbDataProtectionXmlRepository>();
                services.TryAddScoped<IDeploymentStore, LiteDbDeploymentStore>();
                break;

            case StorageProvider.Azure:
                services.TryAddSingleton(_ => CreateCosmosClient(configuration));
                services.TryAddSingleton(serviceProvider => new CosmosDocumentStorage(
                    serviceProvider.GetRequiredService<CosmosClient>(),
                    StorageProviderConfiguration.GetCosmosDatabaseName(configuration),
                    StorageProviderConfiguration.GetCosmosContainerName(configuration)));
                services.TryAddSingleton<IXmlRepository, CosmosDataProtectionXmlRepository>();
                services.TryAddScoped<IDeploymentStore, CosmosDeploymentStore>();
                break;

            case StorageProvider.AWS:
                services.AddDefaultAWSOptions(configuration.GetAWSOptions("Storage:AWS"));
                services.AddAWSService<IAmazonDynamoDB>();
                if (UsesLegacySystemsManagerForDataProtection(configuration))
                {
                    services.AddAWSService<IAmazonSimpleSystemsManagement>();
                }

                services.TryAddSingleton(serviceProvider => new DynamoDbDocumentStorage(
                    serviceProvider.GetRequiredService<IAmazonDynamoDB>(),
                    StorageProviderConfiguration.GetAwsTableName(configuration)));
                services.TryAddSingleton<IXmlRepository, DynamoDbDataProtectionXmlRepository>();
                services.TryAddScoped<IDeploymentStore, DynamoDbDeploymentStore>();
                break;

            case StorageProvider.GCP:
                services.TryAddSingleton(_ => CreateFirestoreDb(configuration));
                services.TryAddSingleton(serviceProvider => new FirestoreDocumentStorage(
                    serviceProvider.GetRequiredService<FirestoreDb>(),
                    StorageProviderConfiguration.GetFirestoreCollectionName(configuration),
                    $"{StorageProviderConfiguration.GetFirestoreCollectionName(configuration)}_data_protection"));
                services.TryAddSingleton<IXmlRepository, FirestoreDataProtectionXmlRepository>();
                services.TryAddScoped<IDeploymentStore, FirestoreDeploymentStore>();
                break;

            default:
                throw new InvalidOperationException("The selected storage provider is not supported.");
        }

        return services;
    }

    /// <summary>
    /// Creates a Cosmos client from the selected Azure provider's connection settings.
    /// </summary>
    private static CosmosClient CreateCosmosClient(IConfiguration configuration)
    {
        var connectionString = configuration["Storage:Azure:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return new CosmosClient(connectionString);
        }

        var endpoint = configuration["Storage:Azure:Endpoint"];
        var key = configuration["Storage:Azure:Key"];
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "Azure storage requires 'Storage:Azure:ConnectionString' or both 'Storage:Azure:Endpoint' and 'Storage:Azure:Key'.");
        }

        return new CosmosClient(endpoint, key);
    }

    /// <summary>
    /// Creates a Firestore client for the selected GCP project.
    /// </summary>
    private static FirestoreDb CreateFirestoreDb(IConfiguration configuration)
    {
        var projectId = configuration["Storage:GCP:ProjectId"];
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException(
                "GCP storage requires 'Storage:GCP:ProjectId'.");
        }

        return FirestoreDb.Create(projectId);
    }

    /// <summary>
    /// Determines whether AWS Data Protection keys are explicitly configured for Systems Manager.
    /// </summary>
    private static bool UsesLegacySystemsManagerForDataProtection(IConfiguration configuration)
    {
        var keyStorage = configuration.GetSection("DataProtection:KeyStorage");
        return !string.IsNullOrWhiteSpace(configuration["Storage:AWS:DataProtectionParameterPath"]) ||
               string.Equals(keyStorage["Provider"], "AWSSystemsManager", StringComparison.OrdinalIgnoreCase) ||
               !string.IsNullOrWhiteSpace(keyStorage["ParameterPath"]);
    }
}

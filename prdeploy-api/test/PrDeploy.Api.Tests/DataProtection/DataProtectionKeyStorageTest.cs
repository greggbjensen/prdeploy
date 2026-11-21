using Amazon.DynamoDBv2;
using Amazon.SimpleSystemsManagement;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PrDeploy.Api;
using PrDeploy.Api.Business;
using PrDeploy.Api.Business.Stores;
using PrDeploy.Api.Business.Stores.Interfaces;
using Microsoft.Azure.Cosmos;
using Google.Cloud.Firestore;

namespace PrDeploy.Api.Tests.DataProtection;

/// <summary>Verifies provider selection and persistence of ASP.NET Core Data Protection keys.</summary>
public class DataProtectionKeyStorageTest
{
    /// <summary>Confirms current and legacy local provider names select the BLite-backed local provider.</summary>
    [Theory]
    [InlineData("Local")]
    [InlineData("BLite")]
    [InlineData("LiteDB")]
    public void StorageProviderConfiguration_MapsLocalAliasesToLocal(string provider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = provider
            })
            .Build();

        Assert.Equal(StorageProvider.Local, StorageProviderConfiguration.GetProvider(configuration));
    }

    /// <summary>Confirms the default provider persists keys locally without registering cloud clients.</summary>
    [Fact]
    public async Task AddPrDeployApi_UsesBliteByDefaultWithoutCloudServicesAndPersistsKeys()
    {
        var directory = Path.Combine(Path.GetTempPath(), "prdeploy-storage-test", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "prdeploy.blite");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Local:DatabasePath"] = databasePath
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPrDeployApi(configuration);
        services.AddPrDeployApiBusiness(configuration);

        try
        {
            string protectedValue;
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var protector = serviceProvider
                    .GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("test");
                protectedValue = protector.Protect("round-trip");

                Assert.Equal("round-trip", protector.Unprotect(protectedValue));
                Assert.IsType<BliteDataProtectionXmlRepository>(
                    serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository);
                // The provider store is wrapped by the logging decorator.
                Assert.IsType<LoggingDeploymentStore>(serviceProvider.GetRequiredService<IDeploymentStore>());
                Assert.NotNull(serviceProvider.GetRequiredService<BliteDeploymentStore>());
                Assert.Null(serviceProvider.GetService<IAmazonSimpleSystemsManagement>());
                Assert.Null(serviceProvider.GetService<IAmazonDynamoDB>());
                Assert.Null(serviceProvider.GetService<CosmosClient>());
                Assert.Null(serviceProvider.GetService<FirestoreDb>());
                Assert.True(File.Exists(databasePath));

                var deploymentStore = serviceProvider.GetRequiredService<IDeploymentStore>();
                await deploymentStore.SetAsync("storage-test", "value");
                Assert.Equal("value", await deploymentStore.GetAsync<string>("storage-test"));
            }

            var restartedServices = new ServiceCollection();
            restartedServices.AddLogging();
            restartedServices.AddPrDeployApi(configuration);
            using (var restartedProvider = restartedServices.BuildServiceProvider())
            {
                var restartedProtector = restartedProvider
                    .GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("test");
                Assert.Equal("round-trip", restartedProtector.Unprotect(protectedValue));
            }
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>Confirms each cloud provider selects its own store without resolving a cloud client.</summary>
    [Theory]
    [InlineData("Azure", typeof(CosmosDeploymentStore))]
    [InlineData("AWS", typeof(DynamoDbDeploymentStore))]
    [InlineData("GCP", typeof(FirestoreDeploymentStore))]
    public void AddPrDeployApiBusiness_RegistersSelectedCloudStore(
        string provider,
        Type expectedStoreType)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = provider
            })
            .Build();
        var services = new ServiceCollection();

        services.AddPrDeployApiBusiness(configuration);

        // The interface is registered through the logging decorator; the provider store is registered by type.
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IDeploymentStore));
        Assert.Single(services, descriptor => descriptor.ServiceType == expectedStoreType);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IXmlRepository));
    }

    /// <summary>Confirms configuration rejects storage provider names that are not supported.</summary>
    [Fact]
    public void AddPrDeployApi_RejectsUnsupportedStorageProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Unsupported"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddPrDeployApi(configuration));

        Assert.Contains("Supported values are 'Local', 'Azure', 'AWS', and 'GCP'.", exception.Message);
    }

    /// <summary>Confirms selecting Cosmos DB requires its connection information when the store is resolved.</summary>
    [Fact]
    public void AddPrDeployApi_RequiresAzureConnectionSettingsWhenSelected()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Azure"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPrDeployApiBusiness(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IDeploymentStore>());

        Assert.Contains("Storage:Azure:ConnectionString", exception.Message);
    }

    /// <summary>Confirms selecting Firestore requires a project identifier when the store is resolved.</summary>
    [Fact]
    public void AddPrDeployApi_RequiresGcpProjectWhenSelected()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "GCP"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPrDeployApiBusiness(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IDeploymentStore>());

        Assert.Contains("Storage:GCP:ProjectId", exception.Message);
    }
}

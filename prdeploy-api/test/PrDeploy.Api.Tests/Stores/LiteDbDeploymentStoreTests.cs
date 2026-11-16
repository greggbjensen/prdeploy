using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Business;
using PrDeploy.Api.Business.Stores.Interfaces;

namespace PrDeploy.Api.Tests.Stores;

/// <summary>
/// Exercises the local deployment store's value serialization and revision history.
/// </summary>
public sealed class LiteDbDeploymentStoreTests
{
    /// <summary>
    /// Confirms local storage round-trips YAML values and retains earlier revisions.
    /// </summary>
    [Fact]
    public async Task DeploymentStore_PreservesYamlValuesAndHistoricalRevisions()
    {
        var directory = Path.Combine(Path.GetTempPath(), "prdeploy-deployment-store-test", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "deployment-values.db");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Provider"] = "Local",
                ["Storage:Local:DatabasePath"] = databasePath,
                ["Storage:DeploymentValuePathPrefix"] = "/prdeploy"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPrDeployApiBusiness(configuration);

        try
        {
            using var serviceProvider = services.BuildServiceProvider();
            var store = serviceProvider.GetRequiredService<IDeploymentStore>();
            await store.SetAsync("octocat", "hello-world", "SETTING", new Dictionary<string, string>
            {
                ["message"] = "first value"
            });
            await store.SetAsync("octocat", "hello-world", "SETTING", new Dictionary<string, string>
            {
                ["message"] = "second value"
            }, isSecure: true);

            Assert.Equal(
                "second value",
                (await store.GetAsync<Dictionary<string, string>>("octocat", "hello-world", "SETTING"))["message"]);
            Assert.Equal(
                "first value",
                (await store.GetHistoricalAsync<Dictionary<string, string>>(
                    "octocat", "hello-world", "SETTING", 1))["message"]);
            Assert.Null(await store.GetHistoricalAsync<string>("octocat", "hello-world", "SETTING", 2));
            Assert.Null(await store.GetAsync<string>("octocat", "missing", "SETTING"));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}

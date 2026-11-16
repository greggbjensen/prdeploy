using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using PrDeploy.Api.Business.Stores;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Tests.Webhooks.Framework;

namespace PrDeploy.Api.Tests.Framework;

/// <summary>
/// Creates an isolated API test host with local storage and an in-memory deployment store.
/// </summary>
public class DeployApiApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _storageDirectory = Path.Combine(
        Path.GetTempPath(),
        "prdeploy-api-tests",
        Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Gets or sets the Configure Services.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public Action<IServiceCollection>? ConfigureServices { get; set; }

    /// <summary>
    /// Gets or sets the Configure Test Services.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public Action<IServiceCollection>? ConfigureTestServices { get; set; }

    /// <summary>
    /// Configures the test host with isolated storage and any test-specific service overrides.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            ConfigureTestServices?.Invoke(services);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<LiteDbStorage>();
            services.AddSingleton(_ => new LiteDbStorage(Path.Combine(_storageDirectory, "prdeploy.db")));
            services.RemoveAll<IDeploymentStore>();
            var deploymentStore = new InMemoryDeploymentStore();
            var defaultSettings = WebhookTestHost.CreateDefaultSettings();
            deploymentStore.SetAsync(WebhookTestHost.Owner, "DEPLOY_SETTINGS", defaultSettings)
                .GetAwaiter().GetResult();
            deploymentStore.SetAsync(
                    WebhookTestHost.Owner, WebhookTestHost.Repo, "DEPLOY_SETTINGS", defaultSettings)
                .GetAwaiter().GetResult();
            services.AddSingleton<IDeploymentStore>(deploymentStore);

            // Services used in all tests.
            // Adds a factory for GraphQL client to use Test Server HttpClient.
            services.AddTestHttpClientFactory(this, options =>
            {
                options.BaseAddress = new Uri("http://localhost/graphql/");
            });

            services.AddDeployApiClient();

            // Call configure services on test.
            ConfigureServices?.Invoke(services);
        });
    }

    /// <summary>
    /// Disposes the test host and removes its temporary local database directory.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_storageDirectory))
        {
            Directory.Delete(_storageDirectory, recursive: true);
        }
    }
}

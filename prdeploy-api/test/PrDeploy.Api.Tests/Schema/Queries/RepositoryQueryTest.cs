using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.OwnerRepo;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Constants;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;
using PrDeploy.Api.Tests.Webhooks.Framework;

namespace PrDeploy.Api.Tests.Schema.Queries;

/// <summary>
/// Contains tests that verify repository query behavior.
/// </summary>
[Collection(ApiHostCollection.Name)]
public class RepositoryQueryTest : DeployApiTest
{
    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    /// <param name="factory">Object containing the factory data used by the operation.</param>
    public RepositoryQueryTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Configures the services for the current application or test.
    /// </summary>
    /// <param name="services">Services to include in the operation.</param>
    public override void ConfigureServices(IServiceCollection services)
    {
        var deploymentStore = new InMemoryDeploymentStore();
        var defaultSettings = WebhookTestHost.CreateDefaultSettings();
        deploymentStore.SetAsync(GitHub.Owner, "DEPLOY_SETTINGS", defaultSettings).GetAwaiter().GetResult();
        deploymentStore.SetAsync(GitHub.Owner, GitHub.Repo, "DEPLOY_SETTINGS", defaultSettings)
            .GetAwaiter().GetResult();
        deploymentStore.SetAsync("OWNER_REPOS", new List<OwnerRepos>
        {
            new() { Owner = GitHub.Owner, Repos = [GitHub.Repo] }
        }).GetAwaiter().GetResult();
        services.AddSingleton<IDeploymentStore>(deploymentStore);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: searches open pull requests.
    /// </summary>
    [Fact]
    public async Task OpenPullRequests_SearchesOpenPullRequests()
    {
        var result = await Client.EnabledOwnerRepos.ExecuteAsync();
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.EnabledOwnerRepos);

        var repositories = result.Data.EnabledOwnerRepos;
        Assert.NotNull(repositories);
        Assert.NotEmpty(repositories);

        var repository = repositories[0];
        Assert.NotNull(repository);
        Assert.Equal("greggbjensen", repository.Owner);
        Assert.Contains("prdeploy-example-repo", repository.Repos);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: lists configured services.
    /// </summary>
    [Fact]
    public async Task RepositoryServices_ListsConfiguredServices()
    {
        var result = await Client.RepositoryServices.ExecuteAsync(new RepoQueryInput
        {
            Owner = GitHub.Owner,
            Repo = GitHub.Repo
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.RepositoryServices);

        var services = result.Data.RepositoryServices;
        Assert.NotNull(services);
        Assert.NotEmpty(services);

        var service = services[0];
        Assert.True(!string.IsNullOrEmpty(service));
    }
}

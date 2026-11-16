using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Schema.Queries;

/// <summary>
/// Contains tests that verify the behavior of the repository Query Test.
/// </summary>
public class RepositoryQueryTest : DeployApiTest
{
    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    /// <param name="factory">Value supplied for factory.</param>
    public RepositoryQueryTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Performs the configure Services operation.
    /// </summary>
    /// <param name="services">Services to include in the operation.</param>
    public override void ConfigureServices(IServiceCollection services)
    {
        // TODO Add SSM mocks.
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: searches Open Pull Requests.
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
    /// Verifies the expected behavior for this scenario: lists Configured Services.
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

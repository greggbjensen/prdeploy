using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Schema.Queries;

public class RepositoryQueryTest : DeployApiTest
{
    public RepositoryQueryTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IGitHubClient>(s =>
        {
            var mock = new Mock<IGitHubClient>(MockBehavior.Strict);

            mock.Setup(x => x.Repository.Content.GetRawContent(
                    GitHub.Owner,
                    GitHub.Repo,
                    PrDeployOptions.RepoSettingsPath
                ))
                .ReturnsAsync(() => File.ReadAllBytes(GitHub.MockRepoSettingsPath));

            mock.Setup(x => x.Repository.Content.GetRawContent(
                    PrDeployOptions.Owner,
                    PrDeployOptions.Repo,
                    PrDeployOptions.DefaultSettingsPath
                ))
                .ReturnsAsync(() => File.ReadAllBytes(GitHub.MockDefaultSettingsPath));

            return mock.Object;
        });
    }

    [Fact]
    public async Task OpenPullRequests_SearchesOpenPullRequests()
    {
        var result = await Client.PrDeployEnabledRepositories.ExecuteAsync();
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.PrDeployEnabledRepositories);

        var repositories = result.Data.PrDeployEnabledRepositories;
        Assert.NotNull(repositories);
        Assert.NotEmpty(repositories);

        var repository = repositories[0];
        Assert.NotNull(repository);
        Assert.Equal("greggbjensen", repository.Owner);
        Assert.Equal("prdeploy-example-repo", repository.Repo);
    }

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

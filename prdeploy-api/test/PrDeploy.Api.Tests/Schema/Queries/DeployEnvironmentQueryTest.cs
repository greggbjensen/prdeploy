using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Schema.Queries;

/// <summary>
/// Contains tests that verify deploy environment query behavior.
/// </summary>
[Collection(ApiHostCollection.Name)]
public class DeployEnvironmentQueryTest : DeployApiTest
{
    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    /// <param name="factory">Object containing the factory data used by the operation.</param>
    public DeployEnvironmentQueryTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Configures the services for the current application or test.
    /// </summary>
    /// <param name="services">Services to include in the operation.</param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IGitHubClient>(s =>
        {
            var mock = new Mock<IGitHubClient>(MockBehavior.Strict);

            mock.Setup(x => x.Issue.Labels.GetAllForRepository(
                    GitHub.Owner,
                    GitHub.Repo
                ))
                .ReturnsAsync(() => new List<Label>
                {
                    Mocker.Label("deployed", "ebeae8"),
                    Mocker.Label("dev", "d4ac0d"),
                    Mocker.Label("dev2", "b7950b"),
                    Mocker.Label("dev3", "9a7d0a"),
                    Mocker.Label("stage", "2e86c1"),
                    Mocker.Label("prod", "1d8348"),
                });

            // TODO Add SSM mocks.

            mock.Setup(x => x.Issue.GetAllForRepository(
                    GitHub.Owner,
                    GitHub.Repo,
                    It.IsAny<RepositoryIssueRequest>(),
                    It.IsAny<ApiOptions>()))
                .ReturnsAsync(new List<Issue>()
                {
                    Mocker.Issue(PullRequests.TestPull, DeployUsers.JohnDoe)
                });

            return mock.Object;
        });
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: returns all environments.
    /// </summary>
    [Fact]
    public async Task DeployEnvironments_ReturnsAllEnvironments()
    {
        var result = await Client.DeployEnvironments.ExecuteAsync(new RepoQueryInput
        {
            Owner = GitHub.Owner, 
            Repo = GitHub.Repo
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.DeployEnvironments);

        var model = result.Data.DeployEnvironments;
        Assert.NotNull(model);
        Assert.NotEmpty(model);

        var dev = model.FirstOrDefault(m => m.Name == "dev");
        Assert.NotNull(dev);
        Assert.Equal(DeployEnvironments.Dev.Name, dev.Name);
        Assert.Equal(DeployEnvironments.Dev.Url, dev.Url);
        Assert.Equal(DeployEnvironments.Dev.Color, dev.Color);

        var pullRequest = dev.PullRequest;
        Assert.NotNull(pullRequest);
        Assert.Equal(PullRequests.TestPull.Number, pullRequest.Number);
        Assert.Equal(PullRequests.TestPull.Title, pullRequest.Title);
        Assert.Equal(PullRequests.TestPull.Body, pullRequest.Body);
        Assert.Equal(PullRequests.TestPull.Url, pullRequest.Url);
        Assert.NotNull(pullRequest.UpdatedAt);

        Assert.NotNull(pullRequest.User);
        Assert.Equal(DeployUsers.JohnDoe.Name, pullRequest.User.Name);
        Assert.Equal(DeployUsers.JohnDoe.Username, pullRequest.User.Username);
    }
}

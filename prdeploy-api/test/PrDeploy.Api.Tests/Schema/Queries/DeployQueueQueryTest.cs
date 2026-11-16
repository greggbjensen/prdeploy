using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Constants;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;
using PrDeploy.Api.Tests.Webhooks.Framework;

namespace PrDeploy.Api.Tests.Schema.Queries;

/// <summary>
/// Contains tests that verify deploy queue query behavior.
/// </summary>
public class DeployQueueQueryTest : DeployApiTest
{
    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    /// <param name="factory">Object containing the factory data used by the operation.</param>
    public DeployQueueQueryTest(DeployApiApplicationFactory factory)
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

            mock.Setup(x => x.PullRequest.Get(
                    GitHub.Owner,
                    GitHub.Repo,
                    PullRequests.TestPull.Number))
                .ReturnsAsync(Mocker.PullRequest(PullRequests.TestPull, DeployUsers.JohnDoe));

            return mock.Object;
        });

        var deploymentStore = new InMemoryDeploymentStore();
        var defaultSettings = WebhookTestHost.CreateDefaultSettings();
        deploymentStore.SetAsync(GitHub.Owner, "DEPLOY_SETTINGS", defaultSettings).GetAwaiter().GetResult();
        deploymentStore.SetAsync(GitHub.Owner, GitHub.Repo, "DEPLOY_SETTINGS", defaultSettings)
            .GetAwaiter().GetResult();
        deploymentStore.SetAsync(GitHub.Owner, GitHub.Repo, DeploymentValueNames.DevPrQueue,
            new List<int> { PullRequests.TestPull.Number }).GetAwaiter().GetResult();
        deploymentStore.SetAsync(GitHub.Owner, GitHub.Repo, DeploymentValueNames.StagePrQueue,
            new List<int>()).GetAwaiter().GetResult();
        deploymentStore.SetAsync(GitHub.Owner, GitHub.Repo, DeploymentValueNames.ProdPrQueue,
            new List<int>()).GetAwaiter().GetResult();
        services.AddSingleton<IDeploymentStore>(deploymentStore);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: returns all queues.
    /// </summary>
    [Fact]
    public async Task DeployQueues_ReturnsAllQueues()
    {
        var result = await Client.DeployQueues.ExecuteAsync(new RepoQueryInput
        {
            Owner = GitHub.Owner,
            Repo = GitHub.Repo
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.DeployQueues);

        var model = result.Data.DeployQueues;
        Assert.NotNull(model);
        Assert.NotEmpty(model);

        var dev = model.FirstOrDefault(m => m.Environment == "dev");
        Assert.NotNull(dev);
        Assert.Equal(DeployEnvironments.Dev.Name, dev.Environment);
        Assert.NotNull(dev.PullRequests);

        var pullRequest = dev.PullRequests.FirstOrDefault(p =>
            p.Number == PullRequests.TestPull.Number);
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

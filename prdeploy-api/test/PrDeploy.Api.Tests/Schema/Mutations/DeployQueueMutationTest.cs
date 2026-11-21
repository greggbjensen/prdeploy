using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Constants;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;
using PrDeploy.Api.Tests.Webhooks.Framework;

namespace PrDeploy.Api.Tests.Schema.Mutations;

/// <summary>Contains tests that verify deploy queue mutation behavior.</summary>
[Collection(ApiHostCollection.Name)]
public class DeployQueueMutationTest : DeployApiTest
{
    /// <summary>Initializes the test fixture and prepares its dependencies.</summary>
    /// <param name="factory">Object containing the factory data used by the operation.</param>
    public DeployQueueMutationTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

    /// <summary>Configures the services for the current application or test.</summary>
    /// <param name="services">Services to include in the operation.</param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(s =>
        {
            var gitHubMock = new Mock<IGitHubClient>(MockBehavior.Strict);

            // TODO Add SSM mocks.

            gitHubMock.Setup(x => x.PullRequest.Get(
                    GitHub.Owner,
                    GitHub.Repo,
                    It.IsAny<int>()))
                .ReturnsAsync(Mocker.PullRequest(PullRequests.TestPull, DeployUsers.JohnDoe));

            gitHubMock.Setup(x => x.Issue.Comment.Create(
                    GitHub.Owner,
                    GitHub.Repo,
                    It.IsAny<long>(),
                    It.IsAny<string>()))
                .ReturnsAsync(() => Mocker.IssueComment());

            return gitHubMock.Object;
        });

        var deploymentStore = new InMemoryDeploymentStore();
        var defaultSettings = WebhookTestHost.CreateDefaultSettings();
        deploymentStore.SetAsync(GitHub.Owner, "DEPLOY_SETTINGS", defaultSettings).GetAwaiter().GetResult();
        deploymentStore.SetAsync(GitHub.Owner, GitHub.Repo, "DEPLOY_SETTINGS", defaultSettings)
            .GetAwaiter().GetResult();
        deploymentStore.SetAsync(GitHub.Owner, GitHub.Repo, DeploymentValueNames.DevPrQueue,
            new List<int> { PullRequests.TestPull.Number }).GetAwaiter().GetResult();
        services.AddSingleton<IDeploymentStore>(deploymentStore);
    }

    /// <summary>Verifies the expected behavior for this scenario: sets environment pull request queue in order.</summary>
    [Fact]
    public async Task DeployQueueUpdate_SetsEnvironmentPullRequestQueueInOrder()
    {
        var result = await Client.DeployQueueUpdate.ExecuteAsync(new DeployQueueUpdateInput
        {
            Owner = GitHub.Owner, 
            Repo = GitHub.Repo,
            Environment = "Dev",
            PullNumbers = new List<int> { 2871, 2852, 2864 }
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.DeployQueueUpdate);

        var model = result.Data.DeployQueueUpdate;
        Assert.Equal("dev", model.Environment);
    }
}

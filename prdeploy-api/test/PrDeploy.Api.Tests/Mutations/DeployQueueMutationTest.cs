using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using PrDeploy.Api.Tests.Framework;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Mutations;

public class DeployQueueMutationTest : DeployApiTest
{
    public DeployQueueMutationTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(s =>
        {
            var gitHubMock = new Mock<IGitHubClient>(MockBehavior.Strict);

            gitHubMock.Setup(x => x.Repository.Content.GetRawContent(
                    GitHub.Owner,
                    GitHub.Repo,
                    GitHub.PrDeploy.RepoSettingsPath
                ))
                .ReturnsAsync(() => File.ReadAllBytes(GitHub.MockRepoSettingsPath));

            gitHubMock.Setup(x => x.Repository.Content.GetRawContent(
                    GitHub.PrDeploy.Owner,
                    GitHub.PrDeploy.Repo,
                    GitHub.PrDeploy.DefaultSettingsPath
                ))
                .ReturnsAsync(() => File.ReadAllBytes(GitHub.MockDefaultSettingsPath));

            gitHubMock.Setup(x => x.PullRequest.Get(
                    GitHub.Owner,
                    GitHub.Repo,
                    PullRequests.TestPull.Number))
                .ReturnsAsync(Mocker.PullRequest(PullRequests.TestPull, DeployUsers.JohnDoe));

            gitHubMock.Setup(x => x.Issue.Comment.Create(
                    GitHub.Owner,
                    GitHub.Repo,
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .ReturnsAsync(() => Mocker.IssueComment());

            return gitHubMock.Object;
        });

        services.AddTransient(s =>
        {
            var awsSsmClient = new Mock<IAmazonSimpleSystemsManagement>(MockBehavior.Strict);

            awsSsmClient.Setup(x => x.PutParameterAsync(
                    It.IsAny<PutParameterRequest>(),
                    default(CancellationToken)))
                .ReturnsAsync(Mocker.PutParameterResponse(
                    ParameterVariables.DevPrQueue,
                    PullRequests.TestPull.Number.ToString()));

            awsSsmClient.Setup(x => x.GetParameterAsync(
                    It.IsAny<GetParameterRequest>(),
                    default(CancellationToken)))
                .ReturnsAsync(Mocker.GetParameterResponse(
                    ParameterVariables.DevPrQueue,
                    $" - {PullRequests.TestPull.Number}"));

            return awsSsmClient.Object;
        });
    }

    [Fact]
    public async Task DeployQueueUpdate_SetsEnvironmentPullRequestQueueInOrder()
    {
        var result = await Client.DeployQueueUpdate.ExecuteAsync(
            GitHub.Owner, GitHub.Repo,
            "Dev", new[] { "2871", "2852", "2864" });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.DeployQueueUpdate);

        var model = result.Data.DeployQueueUpdate;
        Assert.Equal("dev", model.Environment);
    }
}

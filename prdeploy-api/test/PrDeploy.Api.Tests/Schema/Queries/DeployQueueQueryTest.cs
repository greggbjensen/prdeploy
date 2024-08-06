using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Schema.Queries;

public class DeployQueueQueryTest : DeployApiTest
{
    public DeployQueueQueryTest(DeployApiApplicationFactory factory)
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

            mock.Setup(x => x.PullRequest.Get(
                    GitHub.Owner,
                    GitHub.Repo,
                    PullRequests.TestPull.Number))
                .ReturnsAsync(Mocker.PullRequest(PullRequests.TestPull, DeployUsers.JohnDoe));

            return mock.Object;
        });

        services.AddTransient(s =>
        {
            var awsSsmClient = new Mock<IAmazonSimpleSystemsManagement>(MockBehavior.Strict);

            awsSsmClient.Setup(x => x.GetParameterAsync(
                    It.Is<GetParameterRequest>(r => r.Name == ParameterVariables.DevPrQueue),
                    default(CancellationToken)))
                .ReturnsAsync(Mocker.GetParameterResponse(
                    ParameterVariables.DevPrQueue,
                    $" - {PullRequests.TestPull.Number}"));

            awsSsmClient.Setup(x => x.GetParameterAsync(
                    It.Is<GetParameterRequest>(r => r.Name == ParameterVariables.StagePrQueue),
                    default(CancellationToken)))
                .ReturnsAsync(Mocker.GetParameterResponse(
                    ParameterVariables.StagePrQueue,
                    "[]"));

            awsSsmClient.Setup(x => x.GetParameterAsync(
                    It.Is<GetParameterRequest>(r => r.Name == ParameterVariables.ProdPrQueue),
                    default(CancellationToken)))
                .ReturnsAsync(Mocker.GetParameterResponse(
                    ParameterVariables.ProdPrQueue,
                    "[]"));

            return awsSsmClient.Object;
        });
    }

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
            p.Number == PullRequests.TestPull.Number.ToString());
        Assert.NotNull(pullRequest);
        Assert.Equal(PullRequests.TestPull.Number.ToString(), pullRequest.Number);
        Assert.Equal(PullRequests.TestPull.Title, pullRequest.Title);
        Assert.Equal(PullRequests.TestPull.Body, pullRequest.Body);
        Assert.Equal(PullRequests.TestPull.Url, pullRequest.Url);
        Assert.NotNull(pullRequest.UpdatedAt);

        Assert.NotNull(pullRequest.User);
        Assert.Equal(DeployUsers.JohnDoe.Name, pullRequest.User.Name);
        Assert.Equal(DeployUsers.JohnDoe.Username, pullRequest.User.Username);
    }
}

using PrDeploy.Api.Tests.Framework;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Queries;

public class DeployEnvironmentQueryTest : DeployApiTest
{
    public DeployEnvironmentQueryTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

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

            mock.Setup(x => x.Repository.Content.GetRawContent(
                    GitHub.Owner,
                    GitHub.Repo,
                    GitHub.PrDeploy.RepoSettingsPath
                ))
                .ReturnsAsync(() => File.ReadAllBytes(GitHub.MockRepoSettingsPath));

            mock.Setup(x => x.Repository.Content.GetRawContent(
                    GitHub.PrDeploy.Owner,
                    GitHub.PrDeploy.Repo,
                    GitHub.PrDeploy.DefaultSettingsPath
                ))
                .ReturnsAsync(() => File.ReadAllBytes(GitHub.MockDefaultSettingsPath));

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

    [Fact]
    public async Task DeployEnvironments_ReturnsAllEnvironments()
    {
        var result = await Client.DeployEnvironments.ExecuteAsync(GitHub.Owner, GitHub.Repo);
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

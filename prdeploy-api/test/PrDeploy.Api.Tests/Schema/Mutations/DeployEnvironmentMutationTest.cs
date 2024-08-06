using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Schema.Mutations;

public class DeployEnvironmentMutationTest : DeployApiTest
{
    private const int FreePullNumber = 2871;
    private const int DeployPullNumber = 2872;
    private const int ForceDeployPullNumber = 2873;
    private const int RollbackDeployPullNumber = 2874;

    public DeployEnvironmentMutationTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(s =>
        {
            var mock = new Mock<IGitHubClient>(MockBehavior.Strict);

            mock.Setup(x => x.Issue.Comment.Create(
                    GitHub.Owner,
                    GitHub.Repo,
                    FreePullNumber,
                    It.Is<string>(v => v.Trim().StartsWith("/free"))
                ))
                .ReturnsAsync(() => Mocker.IssueComment());


            mock.Setup(x => x.Issue.Comment.Create(
                    GitHub.Owner,
                    GitHub.Repo,
                    DeployPullNumber,
                    It.Is<string>(v =>
                        v.Trim().StartsWith("/deploy stage") && !v.Contains("--force"))
                ))
                .ReturnsAsync(() => Mocker.IssueComment());

            mock.Setup(x => x.Issue.Comment.Create(
                    GitHub.Owner,
                    GitHub.Repo,
                    ForceDeployPullNumber,
                    It.Is<string>(v =>
                        v.Trim().StartsWith("/deploy stage --force"))
                ))
                .ReturnsAsync(() => Mocker.IssueComment());

            mock.Setup(x => x.Issue.Comment.Create(
                    GitHub.Owner,
                    GitHub.Repo,
                    RollbackDeployPullNumber,
                    It.Is<string>(v =>
                        v.Trim().StartsWith("/rollback stage"))
                ))
                .ReturnsAsync(() => Mocker.IssueComment());

            return mock.Object;
        });
    }

    [Fact]
    public async Task DeployEnvironmentFree_AddsFreeCommentToPullRequest()
    {
        var result = await Client.DeployEnvironmentFree.ExecuteAsync(new PullDeployInput
        {
            Owner = GitHub.Owner,
            Repo = GitHub.Repo,
            Environment = "Dev",
            PullNumber = FreePullNumber
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.DeployEnvironmentFree);

        var model = result.Data.DeployEnvironmentFree;
        Assert.True(model.Success);
    }

    [Fact]
    public async Task DeployEnvironmentDeploy_AddsDeployCommentForEnvironmentToPullRequest()
    {
        var result = await Client.DeployEnvironmentDeploy.ExecuteAsync(new ForceDeployInput
        {
            Owner = GitHub.Owner, 
            Repo = GitHub.Repo,
            Environment = "Stage",
            PullNumber = DeployPullNumber,
            Force = false
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.DeployEnvironmentDeploy);

        var model = result.Data.DeployEnvironmentDeploy;
        Assert.True(model.Success);
    }

    [Fact]
    public async Task DeployEnvironmentDeploy_AddsForceDeployCommentForEnvironmentToPullRequest()
    {
        var result = await Client.DeployEnvironmentDeploy.ExecuteAsync(new ForceDeployInput
        {
            Owner = GitHub.Owner, 
            Repo = GitHub.Repo,
            Environment = "Stage", 
            PullNumber = ForceDeployPullNumber,
            Force = true
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.DeployEnvironmentDeploy);

        var model = result.Data.DeployEnvironmentDeploy;
        Assert.True(model.Success);
    }

    [Fact]
    public async Task DeployEnvironmentRollback_AddsRollbackCommentToPullRequest()
    {
        var result = await Client.DeployEnvironmentRollback.ExecuteAsync(new RollbackInput
        {
            Owner = GitHub.Owner,
            Repo = GitHub.Repo,
            Environment = "Stage",
            PullNumber = RollbackDeployPullNumber, 
            Count = 1
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.DeployEnvironmentRollback);

        var model = result.Data.DeployEnvironmentRollback;
        Assert.True(model.Success);
    }
}

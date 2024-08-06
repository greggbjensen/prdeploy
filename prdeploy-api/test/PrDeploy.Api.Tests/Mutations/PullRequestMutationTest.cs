using PrDeploy.Api.Tests.Framework;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Mutations;

public class PullRequestMutationTest : DeployApiTest
{
    private const int AddPullNumber = 2882;

    public PullRequestMutationTest(DeployApiApplicationFactory factory)
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
                        AddPullNumber,
                        It.Is<string>(v => v.Trim().StartsWith("/add deploy-api-test deploy-app-test"))
                    ))
                    .ReturnsAsync(() => Mocker.IssueComment());

                return mock.Object;
            });
        }

    [Fact]
    public async Task PullRequestAddService_AddsAddServiceComment()
    {
        var result = await Client.PullRequestAddServices.ExecuteAsync(new PullRequestAddServicesInput
        {
            Owner = GitHub.Owner,
            Repo = GitHub.Repo,
            PullRequestNumber = AddPullNumber,
            Services = new List<string>{ "deploy-api-test", "deploy-app-test" }
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.PullRequestAddServices);

        var model = result.Data.PullRequestAddServices;
        Assert.True(model.Success);
    }
}

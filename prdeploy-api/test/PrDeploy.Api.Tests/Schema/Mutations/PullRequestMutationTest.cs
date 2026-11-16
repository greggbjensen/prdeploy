using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Schema.Mutations;

/// <summary>
/// Contains tests that verify the behavior of the pull Request Mutation Test.
/// </summary>
public class PullRequestMutationTest : DeployApiTest
{
    private const int AddPullNumber = 2882;

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    /// <param name="factory">Value supplied for factory.</param>
    public PullRequestMutationTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Performs the configure Services operation.
    /// </summary>
    /// <param name="services">Services to include in the operation.</param>
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

    /// <summary>
    /// Verifies the expected behavior for this scenario: adds Add Service Comment.
    /// </summary>
    [Fact]
    public async Task PullRequestAddService_AddsAddServiceComment()
    {
        var result = await Client.PullRequestAddServices.ExecuteAsync(new PullRequestAddServicesInput
        {
            Owner = GitHub.Owner,
            Repo = GitHub.Repo,
            PullNumber = AddPullNumber,
            Services = new List<string>{ "deploy-api-test", "deploy-app-test" }
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.PullRequestAddServices);

        var model = result.Data.PullRequestAddServices;
        Assert.True(model.Success);
    }
}

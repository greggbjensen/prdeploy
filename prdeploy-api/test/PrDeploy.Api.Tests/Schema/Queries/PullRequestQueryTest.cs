using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Schema.Queries;

/// <summary>Contains tests that verify pull request query behavior.</summary>
[Collection(ApiHostCollection.Name)]
public class PullRequestQueryTest : DeployApiTest
{
    /// <summary>Initializes the test fixture and prepares its dependencies.</summary>
    /// <param name="factory">Object containing the factory data used by the operation.</param>
    public PullRequestQueryTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

    /// <summary>Configures the services for the current application or test.</summary>
    /// <param name="services">Services to include in the operation.</param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IGitHubClient>(s =>
        {
            var mock = new Mock<IGitHubClient>(MockBehavior.Strict);

            mock.Setup(x => x.PullRequest.GetAllForRepository(
                    GitHub.Owner,
                    GitHub.Repo,
                    It.IsAny<PullRequestRequest>(),
                    It.IsAny<ApiOptions>()))
                .ReturnsAsync(new List<PullRequest> {
                    Mocker.PullRequest(PullRequests.TestPull, DeployUsers.JohnDoe),
                    Mocker.PullRequest(PullRequests.ListingPull, DeployUsers.SteveWild)
                });

            return mock.Object;
        });
    }

    /// <summary>Verifies the expected behavior for this scenario: searches open pull requests.</summary>
    [Fact]
    public async Task OpenPullRequests_SearchesOpenPullRequests()
    {
        var result = await Client.OpenPullRequests.ExecuteAsync(new OpenPullRequestInput
        {
            Owner = GitHub.Owner, 
            Repo =  GitHub.Repo,
            Search = "listings"
        });
        result.ValidateNoErrors();
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.OpenPullRequests);

        var pullRequests = result.Data.OpenPullRequests;
        Assert.NotNull(pullRequests);
        Assert.NotEmpty(pullRequests);
        Assert.Single(pullRequests);

        var pullRequest = pullRequests[0];
        Assert.NotNull(pullRequest);
        Assert.Equal(PullRequests.ListingPull.Number, pullRequest.Number);
        Assert.Equal(PullRequests.ListingPull.Title, pullRequest.Title);

        Assert.NotNull(pullRequest.User);
        Assert.Equal(DeployUsers.SteveWild.Name, pullRequest.User.Name);
    }
}

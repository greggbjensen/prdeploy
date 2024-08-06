using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Client;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Framework.Client;

namespace PrDeploy.Api.Tests.Schema.Queries;

public class PullRequestQueryTest : DeployApiTest
{
    public PullRequestQueryTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

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
        Assert.Equal(PullRequests.ListingPull.Number.ToString(), pullRequest.Number);
        Assert.Equal(PullRequests.ListingPull.Title, pullRequest.Title);

        Assert.NotNull(pullRequest.User);
        Assert.Equal(DeployUsers.SteveWild.Name, pullRequest.User.Name);
    }
}

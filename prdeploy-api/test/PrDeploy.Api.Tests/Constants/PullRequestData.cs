using PrDeploy.Api.Models;

namespace PrDeploy.Api.Tests.Constants;

public class PullRequestData
{
    public PullRequest TestPull = new()
    {
        Number = 1234,
        Title = "My pull request title",
        Body = "My body for a pull request.",
        Url = "https://github.com/greggbjensen/prdeploy-example-repo/pull/1234"
    };

    public PullRequest ListingPull = new()
    {
        Number = 1235,
        Title = "Update product listings",
        Body = "Fix for product listings not showing.",
        Url = "https://github.com/greggbjensen/prdeploy-example-repo/pull/1235"
    };
}

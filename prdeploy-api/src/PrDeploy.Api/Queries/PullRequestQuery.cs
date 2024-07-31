using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;

namespace PrDeploy.Api.Queries;

[ExtendObjectType("DeployQuery")]
public class PullRequestQuery
{
    [GraphQLName("openPullRequests")]
    public async Task<List<PullRequest>> OpenPullRequests(IPullRequestService service, [ID] string owner, [ID] string repo, string? search) =>
        await service.ListOpenPullRequestsAsync(owner, repo, search);
}

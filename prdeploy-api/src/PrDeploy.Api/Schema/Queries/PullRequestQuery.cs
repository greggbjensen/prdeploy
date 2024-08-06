using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.PullRequests;
using PrDeploy.Api.Models.PullRequests.Inputs;

namespace PrDeploy.Api.Schema.Queries;

[ExtendObjectType("DeployQuery")]
public class PullRequestQuery
{
    [GraphQLName("openPullRequests")]
    public async Task<List<PullRequest>> OpenPullRequests(IPullRequestService service, OpenPullRequestInput input) =>
        await service.ListOpenPullRequestsAsync(input);
}

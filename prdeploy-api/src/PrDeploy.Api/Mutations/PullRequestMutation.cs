using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;

namespace PrDeploy.Api.Mutations;

[ExtendObjectType("DeployMutation")]
public class PullRequestMutation
{
    [GraphQLName("pullRequestAddServices")]
    public async Task<StatusResponse> PullRequestAddServices(IPullRequestService service, [ID] string owner, [ID] string repo,
        [ID] int pullRequestNumber, [ID] List<string> services) =>
        await service.AddServicesAsync(owner, repo, pullRequestNumber, services);
}

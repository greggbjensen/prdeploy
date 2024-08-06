using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.PullRequests.Inputs;

namespace PrDeploy.Api.Schema.Mutations;

[ExtendObjectType("DeployMutation")]
public class PullRequestMutation
{
    [GraphQLName("pullRequestAddServices")]
    public async Task<StatusResponse> PullRequestAddServices(IPullRequestService service, PullRequestAddServicesInput input) =>
        await service.AddServicesAsync(input);
}

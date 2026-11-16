using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.PullRequests.Inputs;

namespace PrDeploy.Api.Schema.Mutations;

/// <summary>
/// Defines mutations for changing the pull Request Mutation.
/// </summary>
[ExtendObjectType("DeployMutation")]
public class PullRequestMutation
{
    /// <summary>
    /// Performs the pull request add services operation and returns its result.
    /// </summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    [GraphQLName("pullRequestAddServices")]
    public async Task<StatusResponse> PullRequestAddServices(IPullRequestService service, PullRequestAddServicesInput input) =>
        await service.AddServicesAsync(input);
}

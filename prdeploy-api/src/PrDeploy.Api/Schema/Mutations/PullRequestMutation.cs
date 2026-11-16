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
    /// Performs the pull Request Add Services operation and returns its result.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("pullRequestAddServices")]
    public async Task<StatusResponse> PullRequestAddServices(IPullRequestService service, PullRequestAddServicesInput input) =>
        await service.AddServicesAsync(input);
}

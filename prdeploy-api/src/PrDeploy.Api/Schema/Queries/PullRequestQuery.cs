using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.PullRequests;
using PrDeploy.Api.Models.PullRequests.Inputs;

namespace PrDeploy.Api.Schema.Queries;

/// <summary>
/// Defines queries for retrieving the pull Request Query.
/// </summary>
[ExtendObjectType("DeployQuery")]
public class PullRequestQuery
{
    /// <summary>
    /// Performs the open Pull Requests operation and returns its result.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("openPullRequests")]
    public async Task<List<PullRequest>> OpenPullRequests(IPullRequestService service, OpenPullRequestInput input) =>
        await service.ListOpenPullRequestsAsync(input);
}

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
    /// Performs the open pull requests operation and returns its result.
    /// </summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The collection of items produced by the operation.</returns>
    [GraphQLName("openPullRequests")]
    public async Task<List<PullRequest>> OpenPullRequests(IPullRequestService service, OpenPullRequestInput input) =>
        await service.ListOpenPullRequestsAsync(input);
}

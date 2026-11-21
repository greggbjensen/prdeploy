using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.PullRequests;
using PrDeploy.Api.Models.PullRequests.Inputs;

namespace PrDeploy.Api.Schema.Queries;

/// <summary>Defines queries for retrieving the pull Request Query.</summary>
[ExtendObjectType("DeployQuery")]
public class PullRequestQuery
{
    /// <summary>Performs the open pull requests operation and returns its result.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The collection of items produced by the operation.</returns>
    [GraphQLName("openPullRequests")]
    public async Task<List<PullRequest>> OpenPullRequests(IPullRequestService service, OpenPullRequestInput input) =>
        await service.ListOpenPullRequestsAsync(input);

    /// <summary>Lists the configured services and whether the pull request already triggers each one.</summary>
    /// <param name="service">Service that reads pull request and settings information.</param>
    /// <param name="input">Repository and pull request number to inspect.</param>
    /// <returns>One entry per configured service.</returns>
    [GraphQLName("pullRequestServices")]
    public async Task<List<PullRequestServiceStatus>> PullRequestServices(IPullRequestService service,
        PullRequestServicesInput input) =>
        await service.ListServicesAsync(input);
}

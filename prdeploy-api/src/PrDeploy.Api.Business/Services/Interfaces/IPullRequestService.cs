using PrDeploy.Api.Models;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.PullRequests;
using PrDeploy.Api.Models.PullRequests.Inputs;

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>Coordinates i pull request operations and their collaborators.</summary>
public interface IPullRequestService
{
    /// <summary>Finds and returns the open pull requests.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>Items matching the supplied criteria.</returns>
    Task<List<PullRequest>> ListOpenPullRequestsAsync(OpenPullRequestInput input);

    /// <summary>Adds the comment command to the associated collection.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <param name="command">Text used as the command.</param>
    /// <returns>The status response produced by the operation.</returns>
    Task<StatusResponse> AddCommentCommandAsync(string owner, string repo, int pullNumber,
        string command);

    /// <summary>Adds the services to the associated collection.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    Task<StatusResponse> AddServicesAsync(PullRequestAddServicesInput input);
}

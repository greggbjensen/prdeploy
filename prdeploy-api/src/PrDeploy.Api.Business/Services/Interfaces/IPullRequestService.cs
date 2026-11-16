using PrDeploy.Api.Models;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.PullRequests;
using PrDeploy.Api.Models.PullRequests.Inputs;

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>
/// Coordinates operations for the i Pull Request Service and its collaborators.
/// </summary>
public interface IPullRequestService
{
    /// <summary>
    /// Finds and returns the Open Pull Requests.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<List<PullRequest>> ListOpenPullRequestsAsync(OpenPullRequestInput input);

    /// <summary>
    /// Adds the Comment Command to the associated collection.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <param name="command">Value supplied for command.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<StatusResponse> AddCommentCommandAsync(string owner, string repo, int pullNumber,
        string command);

    /// <summary>
    /// Adds the Services to the associated collection.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<StatusResponse> AddServicesAsync(PullRequestAddServicesInput input);
}

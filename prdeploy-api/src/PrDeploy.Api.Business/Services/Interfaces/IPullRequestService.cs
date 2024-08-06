using PrDeploy.Api.Models;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.PullRequests;
using PrDeploy.Api.Models.PullRequests.Inputs;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IPullRequestService
{
    Task<List<PullRequest>> ListOpenPullRequestsAsync(OpenPullRequestInput input);

    Task<StatusResponse> AddCommentCommandAsync(string owner, string repo, int pullNumber,
        string command);

    Task<StatusResponse> AddServicesAsync(PullRequestAddServicesInput input);
}

using PrDeploy.Api.Models;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IPullRequestService
{
    Task<List<PullRequest>> ListOpenPullRequestsAsync(string owner, string repo, string? search);

    Task<StatusResponse> AddCommentCommandAsync(string owner, string repo, int pullRequestNumber,
        string command);

    Task<StatusResponse> AddServicesAsync(string owner, string repo, int pullRequestNumber, List<string> services);
}

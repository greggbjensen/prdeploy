using PrDeploy.Api.Models;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IDeployEnvironmentService
{
    Task<List<DeployEnvironment>> ListAsync(string owner, string repo);
    Task<StatusResponse> DeployAsync(string owner, string repo, string environment, int pullRequestNumber, bool force,
        bool retain);

    Task<StatusResponse> RollbackAsync(string owner, string repo, string environment, int pullRequestNumber,
        int count);

    Task<StatusResponse> FreeAsync(string owner, string repo, string environment, int pullRequestNumber);
}

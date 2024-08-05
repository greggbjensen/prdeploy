using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.General;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IDeployEnvironmentService
{
    Task<List<DeployEnvironment>> ListAsync(string owner, string repo);
    Task<StatusResponse> DeployAsync(string owner, string repo, string environment, int pullRequestNumber, bool force,
        bool retain);

    Task<StatusResponse> RollbackAsync(string owner, string repo, string environment, int pullRequestNumber,
        int count);

    Task<StatusResponse> FreeAsync(string owner, string repo, string environment, int pullRequestNumber);
    Task<DeployStateComparison> CompareDeployStateAsync(DeployStateComparisonInput input);
}

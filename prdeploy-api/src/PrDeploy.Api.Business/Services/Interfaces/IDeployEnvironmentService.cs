using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.General.Inputs;
using Environment = PrDeploy.Api.Models.DeployEnvironments.Environment;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IDeployEnvironmentService
{
    Task<List<DeployEnvironment>> ListAsync(RepoQueryInput input);
    Task<StatusResponse> DeployAsync(EnvironmentDeployInput input);

    Task<StatusResponse> RollbackAsync(RollbackInput input);

    Task<StatusResponse> FreeAsync(PullDeployInput input);
    Task<DeployStateComparison> CompareDeployStateAsync(DeployStateComparisonInput input);
    Task<List<Environment>> ListEnvironmentsAsync(RepoQueryInput input);
}

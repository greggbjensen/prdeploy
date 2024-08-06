using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployQueues;
using PrDeploy.Api.Models.DeployQueues.Inputs;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IDeployQueueService
{
    Task<List<DeployQueue>> ListAsync(RepoQueryInput input);
    Task<DeployQueue> UpdateAsync(DeployQueueUpdateInput input);
}

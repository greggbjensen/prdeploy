using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployQueues;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IDeployQueueService
{
    Task<List<DeployQueue>> ListAsync(string owner, string repo);
    Task<DeployQueue> UpdateAsync(string owner, string repo, string environment, List<int>? pullRequestNumbers);
}

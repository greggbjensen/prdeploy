using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployQueues;

namespace PrDeploy.Api.Mutations;

[ExtendObjectType("DeployMutation")]
public class DeployQueueMutation
{
    [GraphQLName("deployQueueUpdate")]
    public async Task<DeployQueue> Update(IDeployQueueService service, [ID] string owner, [ID] string repo, [ID] string environment,
        [ID] List<int>? pullRequestNumbers) => await service.UpdateAsync(owner, repo, environment, pullRequestNumbers);
}

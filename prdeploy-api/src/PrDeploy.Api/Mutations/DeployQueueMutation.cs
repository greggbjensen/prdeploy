using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.DeployQueues;
using PrDeploy.Api.Models.DeployQueues.Inputs;

namespace PrDeploy.Api.Mutations;

[ExtendObjectType("DeployMutation")]
public class DeployQueueMutation
{
    [GraphQLName("deployQueueUpdate")]
    public async Task<DeployQueue> Update(IDeployQueueService service, DeployQueueUpdateInput input) => await service.UpdateAsync(input);
}

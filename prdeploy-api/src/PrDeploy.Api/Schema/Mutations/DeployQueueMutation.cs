using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.DeployQueues;
using PrDeploy.Api.Models.DeployQueues.Inputs;

namespace PrDeploy.Api.Schema.Mutations;

/// <summary>Defines mutations for changing the deploy Queue Mutation.</summary>
[ExtendObjectType("DeployMutation")]
public class DeployQueueMutation
{
    /// <summary>Updates the target value using the supplied inputs.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value after it has been stored or updated.</returns>
    [GraphQLName("deployQueueUpdate")]
    public async Task<DeployQueue> Update(IDeployQueueService service, DeployQueueUpdateInput input) => await service.UpdateAsync(input);
}

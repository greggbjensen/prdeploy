using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.DeployQueues;
using PrDeploy.Api.Models.DeployQueues.Inputs;

namespace PrDeploy.Api.Schema.Mutations;

/// <summary>
/// Defines mutations for changing the deploy Queue Mutation.
/// </summary>
[ExtendObjectType("DeployMutation")]
public class DeployQueueMutation
{
    /// <summary>
    /// Updates the target value using the supplied inputs.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("deployQueueUpdate")]
    public async Task<DeployQueue> Update(IDeployQueueService service, DeployQueueUpdateInput input) => await service.UpdateAsync(input);
}

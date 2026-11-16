using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.DeployQueues;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Schema.Queries;

/// <summary>
/// Defines queries for retrieving the deploy Queue Query.
/// </summary>
[ExtendObjectType("DeployQuery")]
public class DeployQueueQuery
{
    /// <summary>
    /// Performs the deploy Queues operation and returns its result.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("deployQueues")]
    public async Task<List<DeployQueue>> DeployQueues(IDeployQueueService service, RepoQueryInput input) =>
        await service.ListAsync(input);
}

using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.DeployQueues;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Schema.Queries;

/// <summary>Defines queries for retrieving the deploy Queue Query.</summary>
[ExtendObjectType("DeployQuery")]
public class DeployQueueQuery
{
    /// <summary>Performs the deploy queues operation and returns its result.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The collection of items produced by the operation.</returns>
    [GraphQLName("deployQueues")]
    public async Task<List<DeployQueue>> DeployQueues(IDeployQueueService service, RepoQueryInput input) =>
        await service.ListAsync(input);
}

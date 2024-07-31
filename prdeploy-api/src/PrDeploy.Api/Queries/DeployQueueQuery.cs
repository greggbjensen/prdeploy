using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;

namespace PrDeploy.Api.Queries;

[ExtendObjectType("DeployQuery")]
public class DeployQueueQuery
{
    [GraphQLName("deployQueues")]
    public async Task<List<DeployQueue>> DeployQueues(IDeployQueueService service, [ID] string owner, [ID] string repo) =>
        await service.ListAsync(owner, repo);
}

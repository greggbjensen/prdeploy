using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployQueues;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Queries;

[ExtendObjectType("DeployQuery")]
public class DeployQueueQuery
{
    [GraphQLName("deployQueues")]
    public async Task<List<DeployQueue>> DeployQueues(IDeployQueueService service, RepoQueryInput input) =>
        await service.ListAsync(input);
}

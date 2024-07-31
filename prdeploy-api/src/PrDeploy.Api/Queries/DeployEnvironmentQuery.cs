using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;

namespace PrDeploy.Api.Queries;

[ExtendObjectType("DeployQuery")]
public class DeployEnvironmentQuery
{
    [GraphQLName("deployEnvironments")]
    public async Task<List<DeployEnvironment>> DeployEnvironments(IDeployEnvironmentService service, [ID] string owner, [ID] string repo) =>
        await service.ListAsync(owner, repo);
}

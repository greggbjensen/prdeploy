using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;

namespace PrDeploy.Api.Queries;

[ExtendObjectType("DeployQuery")]
public class DeployEnvironmentQuery
{
    [GraphQLName("deployEnvironments")]
    public async Task<List<DeployEnvironment>> DeployEnvironments(IDeployEnvironmentService service, [ID] string owner, [ID] string repo) =>
        await service.ListAsync(owner, repo);

    [GraphQLName("deployStateComparison")]
    public async Task<DeployStateComparison> DeployStateComparison(IDeployEnvironmentService service, DeployStateComparisonInput input) =>
        await service.CompareDeployStateAsync(input);
}

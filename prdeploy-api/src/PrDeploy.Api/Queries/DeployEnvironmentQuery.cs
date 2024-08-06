using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.General.Inputs;
using Environment = PrDeploy.Api.Models.DeployEnvironments.Environment;

namespace PrDeploy.Api.Queries;

[ExtendObjectType("DeployQuery")]
public class DeployEnvironmentQuery
{
    [GraphQLName("deployEnvironments")]
    public async Task<List<DeployEnvironment>> DeployEnvironments(IDeployEnvironmentService service, RepoQueryInput input) =>
        await service.ListAsync(input);

    [GraphQLName("environments")]
    public async Task<List<Environment>> Environments(IDeployEnvironmentService service, RepoQueryInput input) =>
        await service.ListEnvironmentsAsync(input);

    [GraphQLName("deployStateComparison")]
    public async Task<DeployStateComparison> DeployStateComparison(IDeployEnvironmentService service, DeployStateComparisonInput input) =>
        await service.CompareDeployStateAsync(input);
}

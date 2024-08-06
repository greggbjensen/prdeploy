using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.General;

namespace PrDeploy.Api.Schema.Mutations;

[ExtendObjectType("DeployMutation")]
public class DeployEnvironmentMutation
{
    [GraphQLName("deployEnvironmentFree")]
    public async Task<StatusResponse> Free(IDeployEnvironmentService service, PullDeployInput input) => await service.FreeAsync(input);

    [GraphQLName("deployEnvironmentDeploy")]
    public async Task<StatusResponse> ForceDeploy(IDeployEnvironmentService service, EnvironmentDeployInput input) => await service.DeployAsync(input);

    [GraphQLName("deployEnvironmentRollback")]
    public async Task<StatusResponse> Rollback(IDeployEnvironmentService service, RollbackInput input) => await service.RollbackAsync(input);
}

using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using PrDeploy.Api.Models.General;

namespace PrDeploy.Api.Mutations;

[ExtendObjectType("DeployMutation")]
public class DeployEnvironmentMutation
{
    [GraphQLName("deployEnvironmentFree")]
    public async Task<StatusResponse> Free(IDeployEnvironmentService service, [ID] string owner, [ID] string repo, [ID] string environment,
        [ID] int pullRequestNumber) => await service.FreeAsync(owner, repo, environment, pullRequestNumber);

    [GraphQLName("deployEnvironmentDeploy")]
    public async Task<StatusResponse> ForceDeploy(IDeployEnvironmentService service, [ID] string owner, [ID] string repo, [ID] string environment,
        [ID] int pullRequestNumber, bool force = false, bool retain = false) => await service.DeployAsync(owner, repo, environment, pullRequestNumber, force, retain);

    [GraphQLName("deployEnvironmentRollback")]
    public async Task<StatusResponse> Rollback(IDeployEnvironmentService service, [ID] string owner, [ID] string repo, [ID] string environment,
        [ID] int pullRequestNumber, int count = 0) => await service.RollbackAsync(owner, repo, environment, pullRequestNumber, count);
}

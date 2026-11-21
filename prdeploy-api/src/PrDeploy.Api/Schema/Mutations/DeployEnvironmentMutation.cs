using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.General;

namespace PrDeploy.Api.Schema.Mutations;

/// <summary>Defines mutations for changing the deploy Environment Mutation.</summary>
[ExtendObjectType("DeployMutation")]
public class DeployEnvironmentMutation
{
    /// <summary>Releases the resource so it can be used again.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    [GraphQLName("deployEnvironmentFree")]
    public async Task<StatusResponse> Free(IDeployEnvironmentService service, PullDeployInput input) => await service.FreeAsync(input);

    /// <summary>Performs the force deploy operation and returns its result.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    [GraphQLName("deployEnvironmentDeploy")]
    public async Task<StatusResponse> ForceDeploy(IDeployEnvironmentService service, EnvironmentDeployInput input) => await service.DeployAsync(input);

    /// <summary>Restores the deployment to a previous state.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    [GraphQLName("deployEnvironmentRollback")]
    public async Task<StatusResponse> Rollback(IDeployEnvironmentService service, RollbackInput input) => await service.RollbackAsync(input);
}

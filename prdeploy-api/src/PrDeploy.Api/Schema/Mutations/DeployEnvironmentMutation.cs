using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.General;

namespace PrDeploy.Api.Schema.Mutations;

/// <summary>
/// Defines mutations for changing the deploy Environment Mutation.
/// </summary>
[ExtendObjectType("DeployMutation")]
public class DeployEnvironmentMutation
{
    /// <summary>
    /// Releases the requested value so it can be used again.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("deployEnvironmentFree")]
    public async Task<StatusResponse> Free(IDeployEnvironmentService service, PullDeployInput input) => await service.FreeAsync(input);

    /// <summary>
    /// Performs the force Deploy operation and returns its result.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("deployEnvironmentDeploy")]
    public async Task<StatusResponse> ForceDeploy(IDeployEnvironmentService service, EnvironmentDeployInput input) => await service.DeployAsync(input);

    /// <summary>
    /// Restores the requested value to a previous state.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("deployEnvironmentRollback")]
    public async Task<StatusResponse> Rollback(IDeployEnvironmentService service, RollbackInput input) => await service.RollbackAsync(input);
}

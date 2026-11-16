using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Schema.Mutations;

/// <summary>
/// Defines mutations for changing the owner Repo Mutation.
/// </summary>
[ExtendObjectType("DeployMutation")]
public class OwnerRepoMutation
{
    /// <summary>
    /// Adds the enabled to the associated collection.
    /// </summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    [GraphQLName("ownerRepoAddEnabled")]
    public async Task<StatusResponse> AddEnabled(IOwnerRepoService service, Repository input) => 
        await service.AddEnabledAsync(input);

    /// <summary>
    /// Removes the enabled from the associated collection.
    /// </summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    [GraphQLName("ownerRepoRemoveEnabled")]
    public async Task<StatusResponse> RemoveEnabled(IOwnerRepoService service, Repository input) => 
        await service.RemoveEnabledAsync(input);
}

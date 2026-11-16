using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.Settings.Inputs;

namespace PrDeploy.Api.Schema.Mutations;

/// <summary>
/// Defines mutations for changing the deploy Settings Mutation.
/// </summary>
[ExtendObjectType("DeployMutation")]
public class DeploySettingsMutation
{
    /// <summary>
    /// Performs the repo Settings Set operation and returns its result.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("repoSettingsSet")]
    public async Task<StatusResponse> RepoSettingsSet(IDeploySettingsService service, SetRepoSettingsInput input) =>
        await service.SetRepoSettingsAsync(input);

    /// <summary>
    /// Performs the owner Settings Set operation and returns its result.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("ownerSettingsSet")]
    public async Task<StatusResponse> OwnerSettingsSet(IDeploySettingsService service, SetOwnerSettingsInput input) =>
        await service.SetOwnerSettingsAsync(input);
}

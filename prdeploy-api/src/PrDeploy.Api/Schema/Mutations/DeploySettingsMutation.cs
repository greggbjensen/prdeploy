using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.Settings.Inputs;

namespace PrDeploy.Api.Schema.Mutations;

[ExtendObjectType("DeployMutation")]
public class DeploySettingsMutation
{
    [GraphQLName("repoSettingsSet")]
    public async Task<StatusResponse> RepoSettingsSet(IDeploySettingsService service, SetRepoSettingsInput input) =>
        await service.SetRepoSettingsAsync(input);

    [GraphQLName("ownerSettingsSet")]
    public async Task<StatusResponse> OwnerSettingsSet(IDeploySettingsService service, SetOwnerSettingsInput input) =>
        await service.SetOwnerSettingsAsync(input);
}

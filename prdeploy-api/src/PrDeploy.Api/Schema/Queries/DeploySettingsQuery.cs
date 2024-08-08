using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Models.Settings.Compare;

namespace PrDeploy.Api.Schema.Queries;

[ExtendObjectType("DeployQuery")]
public class DeploySettingsQuery
{
    [GraphQLName("deploySettingsCompare")]
    public async Task<DeploySettingsCompare> DeploySettingsCompare(IDeploySettingsService service, RepoQueryInput input) =>
        await service.CompareAsync(input);

    [GraphQLName("repoSettings")]
    public async Task<DeploySettings> RepoSettings(IDeploySettingsService service, RepoQueryInput input) =>
        await service.GetRepoSettingsAsync(input.Owner, input.Repo);

    [GraphQLName("ownerSettings")]
    public async Task<DeploySettings> OwnerSettings(IDeploySettingsService service, OwnerQueryInput input) =>
        await service.GetOwnerSettingsAsync(input.Owner);
}

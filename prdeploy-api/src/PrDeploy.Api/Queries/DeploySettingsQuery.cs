using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings.Compare;

namespace PrDeploy.Api.Queries;

[ExtendObjectType("DeployQuery")]
public class DeploySettingsQuery
{
    [GraphQLName("deploySettingsCompare")]
    public async Task<DeploySettingsCompare> DeploySettingsCompare(IDeploySettingsService service, RepoQueryInput input) =>
        await service.GetAllAsync(input);
}

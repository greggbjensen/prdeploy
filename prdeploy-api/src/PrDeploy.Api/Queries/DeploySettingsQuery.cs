using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Queries;

[ExtendObjectType("DeployQuery")]
public class DeploySettingsQuery
{
    [GraphQLName("allSettings")]
    public async Task<AllSettings> AllSettings(IDeploySettingsService service, RepoQueryInput input) =>
        await service.GetAllAsync(input);
}

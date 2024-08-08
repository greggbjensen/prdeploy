using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Schema.Queries;

[ExtendObjectType("DeployQuery")]
public class RepositoryQuery
{
    [GraphQLName("repositoryServices")]
    public async Task<List<string>> RepositoryServices(IDeploySettingsService service, RepoQueryInput input) =>
        await service.ListServicesAsync(input);
}

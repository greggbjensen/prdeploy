using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Repositories;

namespace PrDeploy.Api.Schema.Queries;

[ExtendObjectType("DeployQuery")]
public class RepositoryQuery
{
    [GraphQLName("prDeployEnabledRepositories")]
    public async Task<List<Repository>> PrDeployEnabledRepositories(IRepositoryService service) =>
        await service.ListPrDeployEnabledAsync();

    [GraphQLName("repositoryServices")]
    public async Task<List<string>> RepositoryServices(IDeploySettingsService service, RepoQueryInput input) =>
        await service.ListServicesAsync(input);
}

using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;

namespace PrDeploy.Api.Queries;

[ExtendObjectType("DeployQuery")]
public class RepositoryQuery
{
    [GraphQLName("prDeployEnabledRepositories")]
    public async Task<List<Repository>> PrDeployEnabledRepositories(IRepositoryService service) =>
        await service.ListPrDeployEnabledAsync();

    [GraphQLName("repositoryServices")]
    public async Task<List<string>> RepositoryServices(IRepoSettingsService service, [ID] string owner,
        [ID] string repo) =>
        await service.ListServicesAsync(owner, repo);
}
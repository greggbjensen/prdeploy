using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Schema.Queries;

[ExtendObjectType("DeployQuery")]
public class OwnerRepoQuery
{
    [GraphQLName("enabledOwnerRepos")]
    public async Task<List<OwnerRepos>> PrDeployEnabledRepositories(IOwnerRepoService service) =>
        await service.ListEnabledAsync();
}

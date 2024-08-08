using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Schema.Queries;

[ExtendObjectType("DeployQuery")]
public class OwnerRepoQuery
{
    [GraphQLName("enabledOwnerRepos")]
    public async Task<List<OwnerRepos>> EnabledOwnerRepos(IOwnerRepoService service) =>
        await service.ListEnabledAsync();
}

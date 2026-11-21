using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Schema.Queries;

/// <summary>Defines queries for retrieving the owner Repo Query.</summary>
[ExtendObjectType("DeployQuery")]
public class OwnerRepoQuery
{
    /// <summary>Performs the enabled owner repos operation and returns its result.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <returns>The collection of items produced by the operation.</returns>
    [GraphQLName("enabledOwnerRepos")]
    public async Task<List<OwnerRepos>> EnabledOwnerRepos(IOwnerRepoService service) =>
        await service.ListEnabledAsync();
}

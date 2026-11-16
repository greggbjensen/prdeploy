using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Schema.Queries;

/// <summary>
/// Defines queries for retrieving the owner Repo Query.
/// </summary>
[ExtendObjectType("DeployQuery")]
public class OwnerRepoQuery
{
    /// <summary>
    /// Performs the enabled Owner Repos operation and returns its result.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("enabledOwnerRepos")]
    public async Task<List<OwnerRepos>> EnabledOwnerRepos(IOwnerRepoService service) =>
        await service.ListEnabledAsync();
}

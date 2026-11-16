using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Schema.Queries;

/// <summary>
/// Defines queries for retrieving the repository Query.
/// </summary>
[ExtendObjectType("DeployQuery")]
public class RepositoryQuery
{
    /// <summary>
    /// Performs the repository Services operation and returns its result.
    /// </summary>
    /// <param name="service">Value supplied for service.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    [GraphQLName("repositoryServices")]
    public async Task<List<string>> RepositoryServices(IDeploySettingsService service, RepoQueryInput input) =>
        await service.ListServicesAsync(input);
}

using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Models.Settings.Compare;

namespace PrDeploy.Api.Schema.Queries;

/// <summary>Defines queries for retrieving the deploy Settings Query.</summary>
[ExtendObjectType("DeployQuery")]
public class DeploySettingsQuery
{
    /// <summary>Performs the deploy settings compare operation and returns its result.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The deploy settings compare produced by the operation.</returns>
    [GraphQLName("deploySettingsCompare")]
    public async Task<DeploySettingsCompare> DeploySettingsCompare(IDeploySettingsService service, RepoQueryInput input) =>
        await service.CompareAsync(input);

    /// <summary>Performs the repo settings operation and returns its result.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The deploy settings produced by the operation.</returns>
    [GraphQLName("repoSettings")]
    public async Task<DeploySettings> RepoSettings(IDeploySettingsService service, RepoQueryInput input) =>
        await service.GetRepoSettingsAsync(input.Owner, input.Repo);

    /// <summary>Performs the owner settings operation and returns its result.</summary>
    /// <param name="service">Object containing the service data used by the operation.</param>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The deploy settings produced by the operation.</returns>
    [GraphQLName("ownerSettings")]
    public async Task<DeploySettings> OwnerSettings(IDeploySettingsService service, OwnerQueryInput input) =>
        await service.GetOwnerSettingsAsync(input.Owner);
}

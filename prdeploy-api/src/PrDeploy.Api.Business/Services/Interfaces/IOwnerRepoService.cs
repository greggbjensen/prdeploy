using PrDeploy.Api.Models;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>
/// Coordinates operations for the i Owner Repo Service and its collaborators.
/// </summary>
public interface IOwnerRepoService
{
    /// <summary>
    /// Finds and returns the Enabled.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    Task<List<OwnerRepos>> ListEnabledAsync();
    /// <summary>
    /// Adds the Enabled to the associated collection.
    /// </summary>
    /// <param name="repository">Repository whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<StatusResponse> AddEnabledAsync(Repository repository);
    /// <summary>
    /// Removes the Enabled from the associated collection.
    /// </summary>
    /// <param name="repository">Repository whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<StatusResponse> RemoveEnabledAsync(Repository repository);
}

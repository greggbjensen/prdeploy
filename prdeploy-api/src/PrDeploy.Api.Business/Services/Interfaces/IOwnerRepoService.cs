using PrDeploy.Api.Models;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>Coordinates i owner repo operations and their collaborators.</summary>
public interface IOwnerRepoService
{
    /// <summary>Finds and returns the enabled.</summary>
    /// <returns>Items matching the supplied criteria.</returns>
    Task<List<OwnerRepos>> ListEnabledAsync();
    /// <summary>Adds the enabled to the associated collection.</summary>
    /// <param name="repository">Repository whose values are addressed.</param>
    /// <returns>The status response produced by the operation.</returns>
    Task<StatusResponse> AddEnabledAsync(Repository repository);
    /// <summary>Removes the enabled from the associated collection.</summary>
    /// <param name="repository">Repository whose values are addressed.</param>
    /// <returns>The status response produced by the operation.</returns>
    Task<StatusResponse> RemoveEnabledAsync(Repository repository);
}

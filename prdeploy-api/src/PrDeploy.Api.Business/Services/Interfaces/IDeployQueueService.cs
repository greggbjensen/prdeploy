using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployQueues;
using PrDeploy.Api.Models.DeployQueues.Inputs;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>
/// Coordinates operations for the i Deploy Queue Service and its collaborators.
/// </summary>
public interface IDeployQueueService
{
    /// <summary>
    /// Finds and returns the matching items.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<List<DeployQueue>> ListAsync(RepoQueryInput input);
    /// <summary>
    /// Updates the target value using the supplied inputs.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<DeployQueue> UpdateAsync(DeployQueueUpdateInput input);
}

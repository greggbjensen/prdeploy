using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.General.Inputs;
using Environment = PrDeploy.Api.Models.DeployEnvironments.Environment;

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>
/// Coordinates operations for the i Deploy Environment Service and its collaborators.
/// </summary>
public interface IDeployEnvironmentService
{
    /// <summary>
    /// Finds and returns the matching items.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<List<DeployEnvironment>> ListAsync(RepoQueryInput input);
    /// <summary>
    /// Performs the deploy operation and returns its result.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<StatusResponse> DeployAsync(EnvironmentDeployInput input);

    /// <summary>
    /// Restores the requested value to a previous state.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<StatusResponse> RollbackAsync(RollbackInput input);

    /// <summary>
    /// Releases the requested value so it can be used again.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<StatusResponse> FreeAsync(PullDeployInput input);
    /// <summary>
    /// Compares the Deploy State and returns the differences.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<DeployStateComparison> CompareDeployStateAsync(DeployStateComparisonInput input);
    /// <summary>
    /// Finds and returns the Environments.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<List<Environment>> ListEnvironmentsAsync(RepoQueryInput input);
}

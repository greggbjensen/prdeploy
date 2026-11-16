using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.General.Inputs;
using Environment = PrDeploy.Api.Models.DeployEnvironments.Environment;

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>
/// Coordinates i deploy environment operations and their collaborators.
/// </summary>
public interface IDeployEnvironmentService
{
    /// <summary>
    /// Finds and returns the matching items.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>Items matching the supplied criteria.</returns>
    Task<List<DeployEnvironment>> ListAsync(RepoQueryInput input);
    /// <summary>
    /// Performs the deploy operation and returns its result.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    Task<StatusResponse> DeployAsync(EnvironmentDeployInput input);

    /// <summary>
    /// Restores the deployment to a previous state.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    Task<StatusResponse> RollbackAsync(RollbackInput input);

    /// <summary>
    /// Releases the resource so it can be used again.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    Task<StatusResponse> FreeAsync(PullDeployInput input);
    /// <summary>
    /// Compares the deploy state and returns their differences.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The deploy state comparison produced by the operation.</returns>
    Task<DeployStateComparison> CompareDeployStateAsync(DeployStateComparisonInput input);
    /// <summary>
    /// Finds and returns the environments.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>Items matching the supplied criteria.</returns>
    Task<List<Environment>> ListEnvironmentsAsync(RepoQueryInput input);
}

using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Models.Settings.Compare;
using PrDeploy.Api.Models.Settings.Inputs;

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>
/// Coordinates operations for the i Deploy Settings Service and its collaborators.
/// </summary>
public interface IDeploySettingsService
{
    /// <summary>
    /// Retrieves the Merged.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<DeploySettings> GetMergedAsync(string owner, string repo);
    /// <summary>
    /// Retrieves the Environment.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<EnvironmentSettings> GetEnvironmentAsync(string owner, string repo, string environment);
    /// <summary>
    /// Retrieves the Queue Environments.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<List<EnvironmentSettings>> GetQueueEnvironmentsAsync(string owner, string repo);

    /// <summary>
    /// Retrieves the Environment.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="deploySettings">Value supplied for deploy Settings.</param>
    /// <returns>The value produced by this operation.</returns>
    EnvironmentSettings? GetEnvironment(string owner, string repo, string environment,
        DeploySettings deploySettings);

    /// <summary>
    /// Finds and returns the Services.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<List<string>> ListServicesAsync(RepoQueryInput input);
    /// <summary>
    /// Compares the requested value and returns the differences.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<DeploySettingsCompare> CompareAsync(RepoQueryInput input);
    /// <summary>
    /// Retrieves the Owner Settings.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<DeploySettings> GetOwnerSettingsAsync(string owner);
    /// <summary>
    /// Retrieves the Repo Settings.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<DeploySettings> GetRepoSettingsAsync(string owner, string repo);
    /// <summary>
    /// Stores or updates the Owner Settings.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<StatusResponse> SetOwnerSettingsAsync(SetOwnerSettingsInput input);
    /// <summary>
    /// Stores or updates the Repo Settings.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<StatusResponse> SetRepoSettingsAsync(SetRepoSettingsInput input);
}

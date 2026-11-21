using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Models.Settings.Compare;
using PrDeploy.Api.Models.Settings.Inputs;

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>Coordinates i deploy settings operations and their collaborators.</summary>
public interface IDeploySettingsService
{
    /// <summary>Retrieves the merged.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    Task<DeploySettings> GetMergedAsync(string owner, string repo);
    /// <summary>Retrieves the environment.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    Task<EnvironmentSettings> GetEnvironmentAsync(string owner, string repo, string environment);
    /// <summary>Retrieves the queue environments.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    Task<List<EnvironmentSettings>> GetQueueEnvironmentsAsync(string owner, string repo);

    /// <summary>Retrieves the environment.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="deploySettings">Object containing the deploy settings data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    EnvironmentSettings? GetEnvironment(string owner, string repo, string environment,
        DeploySettings deploySettings);

    /// <summary>Finds and returns the services.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>Items matching the supplied criteria.</returns>
    Task<List<string>> ListServicesAsync(RepoQueryInput input);
    /// <summary>Compares the supplied values and returns their differences.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The deploy settings compare produced by the operation.</returns>
    Task<DeploySettingsCompare> CompareAsync(RepoQueryInput input);
    /// <summary>Retrieves the owner settings.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    Task<DeploySettings> GetOwnerSettingsAsync(string owner);
    /// <summary>Retrieves the repo settings.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    Task<DeploySettings> GetRepoSettingsAsync(string owner, string repo);
    /// <summary>Stores or updates the owner settings.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value after it has been stored or updated.</returns>
    Task<StatusResponse> SetOwnerSettingsAsync(SetOwnerSettingsInput input);
    /// <summary>Stores or updates the repo settings.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value after it has been stored or updated.</returns>
    Task<StatusResponse> SetRepoSettingsAsync(SetRepoSettingsInput input);
}

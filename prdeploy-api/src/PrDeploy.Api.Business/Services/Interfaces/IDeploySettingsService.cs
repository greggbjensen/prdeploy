using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Models.Settings.Compare;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IDeploySettingsService
{
    Task<DeploySettings> GetMergedAsync(string owner, string repo);
    Task<EnvironmentSettings> GetEnvironmentAsync(string owner, string repo, string environment);
    Task<List<EnvironmentSettings>> GetQueueEnvironmentsAsync(string owner, string repo);

    EnvironmentSettings? GetEnvironment(string owner, string repo, string environment,
        DeploySettings deploySettings);

    Task<List<string>> ListServicesAsync(RepoQueryInput input);
    Task<DeploySettingsCompare> GetAllAsync(RepoQueryInput input);
    Task<DeploySettings> GetOwnerSettingsAsync(string owner);
    Task<DeploySettings> GetRepoSettingsAsync(string owner, string repo);
}

using PrDeploy.Api.Business.Models.Settings;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IRepoSettingsService
{
    Task<RepoSettings> GetAsync(string owner, string repo);
    Task<EnvironmentSettings> GetEnvironmentAsync(string owner, string repo, string environment);
    Task<List<EnvironmentSettings>> GetQueueEnvironmentsAsync(string owner, string repo);

    EnvironmentSettings? GetEnvironment(string owner, string repo, string environment,
        RepoSettings repoSettings);

    Task<List<string>> ListServicesAsync(string owner, string repo);
}

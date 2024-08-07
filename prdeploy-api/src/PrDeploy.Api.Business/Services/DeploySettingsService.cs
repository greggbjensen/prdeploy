using System.Net;
using System.Text.RegularExpressions;
using PrDeploy.Api.Business.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Octokit;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Models.Settings.Compare;
using PrDeploy.Api.Models.Settings.Inputs;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace PrDeploy.Api.Business.Services;
public class DeploySettingsService : IDeploySettingsService
{
    private const string DeploySettingsKey = "DEPLOY_SETTINGS";
    private static readonly TimeSpan RepoSettingsExpiration = TimeSpan.FromMinutes(5);
    private static readonly Regex NormalizeEnvironmentRegex = new Regex(@"\d+$", RegexOptions.Compiled);
    private static DeploySettings? _defaultOwnerSettings;
    private readonly IMemoryCache _cache;

    private readonly IParameterStore _parameterStore;
    private readonly IGitHubSecurity _gitHubSecurity;

    public DeploySettingsService(IParameterStore parameterStore, IGitHubSecurity gitHubSecurity, IMemoryCache cache)
    {
        _parameterStore = parameterStore;
        _gitHubSecurity = gitHubSecurity;
        _cache = cache;
    }

    public async Task<DeploySettingsCompare> GetAllAsync(RepoQueryInput input)
    {
        // Add validation.
        var ownerSettings = await GetOwnerSettingsAsync(input.Owner);
        var repoSettings = await GetRepoSettingsAsync(input.Owner, input.Repo);

        var settingsCompare = Map.Compare(ownerSettings, repoSettings);
        return settingsCompare!;
    }

    public async Task<DeploySettings> GetMergedAsync(string owner, string repo)
    {
        var repoKey = GetSettingsCacheKey(owner, repo);
        var repoSettings = _cache.Get<DeploySettings>(repoKey);
        if (repoSettings != null)
        {
            return repoSettings;
        }

        var ownerSettings = await GetOwnerSettingsAsync(owner);
        repoSettings = await GetRepoSettingsAsync(owner, repo);

        repoSettings.Owner = owner;
        repoSettings.Repo = repo;

        // Override default with repo.
        Map.Merge(repoSettings, ownerSettings);
        _cache.Set(repoKey, repoSettings, RepoSettingsExpiration);

        return repoSettings;
    }

    public async Task<EnvironmentSettings> GetEnvironmentAsync(string owner, string repo, string environment)
    {
        var repoSettings = await GetMergedAsync(owner, repo);
        var result = GetEnvironment(owner, repo, environment, repoSettings);
        return result;
    }

    public EnvironmentSettings? GetEnvironment(string owner, string repo, string environment,
        DeploySettings deploySettings)
    {
        var result = deploySettings.Environments!
            .Find(e => string.Equals(e.Name, environment, StringComparison.OrdinalIgnoreCase));
        if (result == null)
        {
            throw new NotFoundException(
                $"Environment {environment} did not have a settings entry in repository {owner}/{repo}.",
                HttpStatusCode.NotFound);
        }

        return result;
    }

    public async Task<List<EnvironmentSettings>> GetQueueEnvironmentsAsync(string owner, string repo)
    {
        var repoSettings = await GetMergedAsync(owner, repo);
        var environmentSettings = repoSettings.Environments!
            .GroupBy(e => e.Queue).Select(g =>
            {
                var environment = g.First();
                environment.Name = NormalizeEnvironment(environment.Name);
                return environment;
            }).ToList();

        return environmentSettings;
    }

    public async Task<List<string>> ListServicesAsync(RepoQueryInput input)
    {
        List<string>? services = null;

        var settings = await GetMergedAsync(input.Owner, input.Repo);
        if (settings.Services?.Any() == true)
        {
            services = settings.Services.Select(s => s.Name).ToList();
        }

        return services ?? new List<string>();
    }

    public async Task<DeploySettings> GetOwnerSettingsAsync(string owner)
    {
        await _gitHubSecurity.GuardOwnerAsync(owner);

        // Repo specific settings, which are optional.
        var deploySettings = await _parameterStore.GetAsync<DeploySettings?>(owner, DeploySettingsKey);
        if (deploySettings == null)
        {
            _defaultOwnerSettings ??= await LoadDefaultOwnerSettingsAsync();
            deploySettings = _defaultOwnerSettings;
            await _parameterStore.SetAsync(owner, DeploySettingsKey, deploySettings, true);
        }

        return deploySettings;
    }

    public async Task<StatusResponse> SetOwnerSettingsAsync(SetOwnerSettingsInput input)
    {
        await _gitHubSecurity.GuardOwnerAsync(input.Owner);

        await _parameterStore.SetAsync(input.Owner, DeploySettingsKey, input.Settings, true);

        return new StatusResponse
        {
            Success = true
        };
    }

    public async Task<DeploySettings> GetRepoSettingsAsync(string owner, string repo)
    {
        await _gitHubSecurity.GuardRepoAsync(owner, repo);

        // Repo specific settings, which are optional.
        var deploySettings = await _parameterStore.GetAsync<DeploySettings?>(owner, repo, DeploySettingsKey);
        if (deploySettings == null)
        {
            deploySettings = new DeploySettings();
            await _parameterStore.SetAsync(owner, repo, DeploySettingsKey, deploySettings, true);
        }

        return deploySettings;
    }

    public async Task<StatusResponse> SetRepoSettingsAsync(SetRepoSettingsInput input)
    {
        await _gitHubSecurity.GuardRepoAsync(input.Owner, input.Repo);

        await _parameterStore.SetAsync(input.Owner, input.Repo, DeploySettingsKey, input.Settings, true);

        // Clear cache so it can update.
        var cacheKy = GetSettingsCacheKey(input.Owner, input.Repo);
        _cache.Remove(cacheKy);

        return new StatusResponse
        {
            Success = true
        };
    }

    public string NormalizeEnvironment(string environment)
    {
        return NormalizeEnvironmentRegex.Replace(environment, string.Empty);
    }

    private async Task<DeploySettings> LoadDefaultOwnerSettingsAsync()
    {
        var yaml = await File.ReadAllTextAsync("default-owner-settings.yaml");
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var deploySettings = deserializer.Deserialize<DeploySettings>(yaml);
        return deploySettings;
    }

    private static string GetSettingsCacheKey(string owner, string repo) => $"{owner}:{repo}:settings";
}

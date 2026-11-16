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
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrDeploy.Api.Business.Services;
/// <summary>
/// Coordinates operations for the deploy Settings Service and its collaborators.
/// </summary>
public class DeploySettingsService : IDeploySettingsService
{
    private const string DeploySettingsKey = "DEPLOY_SETTINGS";
    private static readonly TimeSpan RepoSettingsExpiration = TimeSpan.FromMinutes(5);
    private static readonly Regex NormalizeEnvironmentRegex = new Regex(@"\d+$", RegexOptions.Compiled);
    private static DeploySettings? _defaultOwnerSettings;
    private readonly IMemoryCache _cache;

    private readonly IDeploymentStore _deploymentStore;
    private readonly IGitHubSecurity _gitHubSecurity;

    /// <summary>
    /// Initializes a deploy Settings Service with the supplied dependencies.
    /// </summary>
    /// <param name="deploymentStore">Value supplied for deployment Store.</param>
    /// <param name="gitHubSecurity">Value supplied for git Hub Security.</param>
    /// <param name="cache">Value supplied for cache.</param>
    public DeploySettingsService(IDeploymentStore deploymentStore, IGitHubSecurity gitHubSecurity, IMemoryCache cache)
    {
        _deploymentStore = deploymentStore;
        _gitHubSecurity = gitHubSecurity;
        _cache = cache;
    }

    /// <summary>
    /// Compares the requested value and returns the differences.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<DeploySettingsCompare> CompareAsync(RepoQueryInput input)
    {
        // Add validation.
        var ownerSettings = await GetOwnerSettingsAsync(input.Owner);
        var repoSettings = await GetRepoSettingsAsync(input.Owner, input.Repo);

        var settingsCompare = Map.Compare(ownerSettings, repoSettings);
        return settingsCompare!;
    }

    /// <summary>
    /// Retrieves the Merged.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<DeploySettings> GetMergedAsync(string owner, string repo)
    {
        var repoKey = GetSettingsCacheKey(owner, repo);
        var repoSettings = _cache.Get<DeploySettings>(repoKey);
        if (repoSettings != null)
        {
            await _gitHubSecurity.GuardRepoAsync(owner, repo);
            return repoSettings;
        }

        var ownerSettings = await GetOwnerSettingsAsync(owner);
        repoSettings = await GetRepoSettingsAsync(owner, repo);

        // Override default with repo.
        Map.Merge(repoSettings, ownerSettings);
        _cache.Set(repoKey, repoSettings, RepoSettingsExpiration);

        return repoSettings;
    }

    /// <summary>
    /// Retrieves the Environment.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<EnvironmentSettings> GetEnvironmentAsync(string owner, string repo, string environment)
    {
        var repoSettings = await GetMergedAsync(owner, repo);
        var result = GetEnvironment(owner, repo, environment, repoSettings);
        return result!;
    }

    /// <summary>
    /// Retrieves the Environment.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="deploySettings">Value supplied for deploy Settings.</param>
    /// <returns>The value produced by this operation.</returns>
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

    /// <summary>
    /// Retrieves the Queue Environments.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
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

    /// <summary>
    /// Finds and returns the Services.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
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

    /// <summary>
    /// Retrieves the Owner Settings.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<DeploySettings> GetOwnerSettingsAsync(string owner)
    {
        await _gitHubSecurity.GuardOwnerAsync(owner);

        // Repo specific settings, which are optional.
        var deploySettings = await _deploymentStore.GetAsync<DeploySettings?>(owner, DeploySettingsKey);
        if (deploySettings == null)
        {
            _defaultOwnerSettings ??= await LoadDefaultOwnerSettingsAsync();
            deploySettings = _defaultOwnerSettings;
            await _deploymentStore.SetAsync(owner, DeploySettingsKey, deploySettings, true);
        }

        return deploySettings;
    }

    /// <summary>
    /// Stores or updates the Owner Settings.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<StatusResponse> SetOwnerSettingsAsync(SetOwnerSettingsInput input)
    {
        await _gitHubSecurity.GuardOwnerAsync(input.Owner);

        await _deploymentStore.SetAsync(input.Owner, DeploySettingsKey, input.Settings, true);

        return new StatusResponse
        {
            Success = true
        };
    }

    /// <summary>
    /// Retrieves the Repo Settings.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<DeploySettings> GetRepoSettingsAsync(string owner, string repo)
    {
        await _gitHubSecurity.GuardRepoAsync(owner, repo);

        // Repo specific settings, which are optional.
        var deploySettings = await _deploymentStore.GetAsync<DeploySettings?>(owner, repo, DeploySettingsKey);
        if (deploySettings == null)
        {
            deploySettings = new DeploySettings();
            await _deploymentStore.SetAsync(owner, repo, DeploySettingsKey, deploySettings, true);
        }

        return deploySettings;
    }

    /// <summary>
    /// Stores or updates the Repo Settings.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<StatusResponse> SetRepoSettingsAsync(SetRepoSettingsInput input)
    {
        await _gitHubSecurity.GuardRepoAsync(input.Owner, input.Repo);

        await _deploymentStore.SetAsync(input.Owner, input.Repo, DeploySettingsKey, input.Settings, true);

        // Clear cache so it can update, including the settings used by webhooks.
        _cache.Remove(GetSettingsCacheKey(input.Owner, input.Repo));
        _cache.Remove(GetWebhookSettingsCacheKey(input.Owner, input.Repo));

        return new StatusResponse
        {
            Success = true
        };
    }

    /// <summary>
    /// Normalizes the Environment for consistent processing.
    /// </summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public string NormalizeEnvironment(string environment)
    {
        return NormalizeEnvironmentRegex.Replace(environment, string.Empty);
    }

    /// <summary>
    /// Performs the load Default Owner Settings operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private async Task<DeploySettings> LoadDefaultOwnerSettingsAsync()
    {
        var yaml = await File.ReadAllTextAsync("default-owner-settings.yaml");
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var deploySettings = deserializer.Deserialize<DeploySettings>(yaml);
        return deploySettings;
    }

    /// <summary>
    /// Retrieves the Settings Cache Key.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string GetSettingsCacheKey(string owner, string repo) => $"{owner}:{repo}:settings";

    /// <summary>
    /// Retrieves the Webhook Settings Cache Key.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>The value produced by this operation.</returns>
    public static string GetWebhookSettingsCacheKey(string owner, string repo) => $"{owner}:{repo}:webhook-settings";
}

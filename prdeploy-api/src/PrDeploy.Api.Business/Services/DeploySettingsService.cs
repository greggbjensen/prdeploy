using System.Net;
using System.Text.RegularExpressions;
using PrDeploy.Api.Business.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
/// <summary>Coordinates deploy settings operations and their collaborators.</summary>
public class DeploySettingsService : IDeploySettingsService
{
    private const string DeploySettingsKey = "DEPLOY_SETTINGS";
    private static readonly TimeSpan RepoSettingsExpiration = TimeSpan.FromMinutes(5);
    private static readonly Regex NormalizeEnvironmentRegex = new Regex(@"\d+$", RegexOptions.Compiled);
    private static DeploySettings? _defaultOwnerSettings;
    private readonly IMemoryCache _cache;

    private readonly IDeploymentStore _deploymentStore;
    private readonly IGitHubSecurity _gitHubSecurity;
    private readonly ILogger<DeploySettingsService> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="deploymentStore">Object containing the deployment store data used by the operation.</param>
    /// <param name="gitHubSecurity">Object containing the git hub security data used by the operation.</param>
    /// <param name="cache">Object containing the cache data used by the operation.</param>
    /// <param name="logger">Logger used to report operational activity.</param>
    public DeploySettingsService(IDeploymentStore deploymentStore, IGitHubSecurity gitHubSecurity, IMemoryCache cache,
        ILogger<DeploySettingsService> logger)
    {
        _deploymentStore = deploymentStore;
        _gitHubSecurity = gitHubSecurity;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Compares the supplied values and returns their differences.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The deploy settings compare produced by the operation.</returns>
    public async Task<DeploySettingsCompare> CompareAsync(RepoQueryInput input)
    {
        // Repository access is the requirement here; the owner defaults are only read for comparison,
        // so outside collaborators who are not organization members can still compare settings.
        var repoSettings = await LoadRepoSettingsAsync(input.Owner, input.Repo);
        var ownerSettings = await LoadOwnerSettingsAsync(input.Owner);

        // Owner credentials are never returned, so outside collaborators cannot read the organization's tokens.
        var settingsCompare = Map.Compare(Map.RedactSecrets(ownerSettings), Map.RedactSecrets(repoSettings));
        return settingsCompare!;
    }

    /// <summary>Retrieves the merged.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public async Task<DeploySettings> GetMergedAsync(string owner, string repo)
    {
        // Merged settings are repository scoped, so only repository access is required. Guarding the owner
        // here would reject outside collaborators, who are not members of the owning organization.
        await _gitHubSecurity.GuardRepoAsync(owner, repo);

        var repoKey = GetSettingsCacheKey(owner, repo);
        var repoSettings = _cache.Get<DeploySettings>(repoKey);
        if (repoSettings != null)
        {
            _logger.LogDebug("Using cached merged settings for {Owner}/{Repo}.", owner, repo);
            return repoSettings;
        }

        _logger.LogDebug("Merging owner and repo settings for {Owner}/{Repo}.", owner, repo);
        var ownerSettings = await LoadOwnerSettingsAsync(owner);
        repoSettings = await LoadRepoSettingsAsync(owner, repo);

        // Override default with repo.
        Map.Merge(repoSettings, ownerSettings);
        _cache.Set(repoKey, repoSettings, RepoSettingsExpiration);

        return repoSettings;
    }

    /// <summary>Retrieves the environment.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public async Task<EnvironmentSettings> GetEnvironmentAsync(string owner, string repo, string environment)
    {
        var repoSettings = await GetMergedAsync(owner, repo);
        var result = GetEnvironment(owner, repo, environment, repoSettings);
        return result!;
    }

    /// <summary>Retrieves the environment.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="deploySettings">Object containing the deploy settings data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public EnvironmentSettings? GetEnvironment(string owner, string repo, string environment,
        DeploySettings deploySettings)
    {
        var result = deploySettings.Environments!
            .Find(e => string.Equals(e.Name, environment, StringComparison.OrdinalIgnoreCase));
        if (result == null)
        {
            _logger.LogWarning("Environment {Environment} has no settings entry in {Owner}/{Repo}.",
                environment, owner, repo);
            throw new NotFoundException(
                $"Environment {environment} did not have a settings entry in repository {owner}/{repo}.",
                HttpStatusCode.NotFound);
        }

        return result;
    }

    /// <summary>Retrieves the queue environments.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public async Task<List<EnvironmentSettings>> GetQueueEnvironmentsAsync(string owner, string repo)
    {
        var repoSettings = await GetMergedAsync(owner, repo);
        // Copy the first environment of each queue, because the merged settings are cached and shared.
        var environmentSettings = repoSettings.Environments!
            .GroupBy(e => e.Queue).Select(g =>
            {
                var environment = g.First();
                return new EnvironmentSettings
                {
                    Name = NormalizeEnvironment(environment.Name),
                    Queue = environment.Queue,
                    Url = environment.Url,
                    Urls = environment.Urls,
                    Color = environment.Color,
                    RequireApproval = environment.RequireApproval,
                    RequireBranchUpToDate = environment.RequireBranchUpToDate,
                    AutomationTest = environment.AutomationTest,
                    ExcludeFromRollback = environment.ExcludeFromRollback
                };
            }).ToList();

        return environmentSettings;
    }

    /// <summary>Finds and returns the services.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>Items matching the supplied criteria.</returns>
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

    /// <summary>Retrieves the owner settings.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public async Task<DeploySettings> GetOwnerSettingsAsync(string owner)
    {
        await _gitHubSecurity.GuardOwnerAsync(owner);
        return Map.RedactSecrets(await LoadOwnerSettingsAsync(owner));
    }

    /// <summary>Loads the owner level settings without an access check, saving the defaults when none exist yet.</summary>
    /// <remarks>
    /// Callers must have already guarded access, either to the owner or to a repository under the owner.
    /// </remarks>
    /// <param name="owner">Repository owner or organization whose settings are loaded.</param>
    /// <returns>The stored owner settings, or the defaults when none were stored.</returns>
    private async Task<DeploySettings> LoadOwnerSettingsAsync(string owner)
    {
        // Owner settings, which fall back to the defaults.
        var deploySettings = await _deploymentStore.GetAsync<DeploySettings?>(owner, DeploySettingsKey);
        if (deploySettings == null)
        {
            _logger.LogInformation("No settings stored for owner {Owner}, saving the defaults.", owner);
            _defaultOwnerSettings ??= await LoadDefaultOwnerSettingsAsync();
            deploySettings = _defaultOwnerSettings;
            await _deploymentStore.SetAsync(owner, DeploySettingsKey, deploySettings, true);
        }

        return deploySettings;
    }

    /// <summary>Stores or updates the owner settings.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value after it has been stored or updated.</returns>
    public async Task<StatusResponse> SetOwnerSettingsAsync(SetOwnerSettingsInput input)
    {
        await _gitHubSecurity.GuardOwnerAsync(input.Owner);

        Map.RestoreSecrets(input.Settings,
            await _deploymentStore.GetAsync<DeploySettings?>(input.Owner, DeploySettingsKey));
        await _deploymentStore.SetAsync(input.Owner, DeploySettingsKey, input.Settings, true);
        _logger.LogInformation("Updated owner settings for {Owner}.", input.Owner);

        return new StatusResponse
        {
            Success = true
        };
    }

    /// <summary>Retrieves the repo settings with credentials masked.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public async Task<DeploySettings> GetRepoSettingsAsync(string owner, string repo)
    {
        return Map.RedactSecrets(await LoadRepoSettingsAsync(owner, repo));
    }

    /// <summary>Loads the stored repository settings, including secrets, after checking repository access.</summary>
    /// <param name="owner">Owner of the repository.</param>
    /// <param name="repo">Name of the repository.</param>
    /// <returns>The stored settings, or empty settings that are saved when none exist.</returns>
    private async Task<DeploySettings> LoadRepoSettingsAsync(string owner, string repo)
    {
        await _gitHubSecurity.GuardRepoAsync(owner, repo);

        // Repo specific settings, which are optional.
        var deploySettings = await _deploymentStore.GetAsync<DeploySettings?>(owner, repo, DeploySettingsKey);
        if (deploySettings == null)
        {
            _logger.LogInformation("No settings stored for {Owner}/{Repo}, saving empty settings.", owner, repo);
            deploySettings = new DeploySettings();
            await _deploymentStore.SetAsync(owner, repo, DeploySettingsKey, deploySettings, true);
        }

        return deploySettings;
    }

    /// <summary>Stores or updates the repo settings.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value after it has been stored or updated.</returns>
    public async Task<StatusResponse> SetRepoSettingsAsync(SetRepoSettingsInput input)
    {
        await _gitHubSecurity.GuardRepoAsync(input.Owner, input.Repo);

        Map.RestoreSecrets(input.Settings,
            await _deploymentStore.GetAsync<DeploySettings?>(input.Owner, input.Repo, DeploySettingsKey));
        await _deploymentStore.SetAsync(input.Owner, input.Repo, DeploySettingsKey, input.Settings, true);

        _logger.LogInformation("Updated repo settings for {Owner}/{Repo}.", input.Owner, input.Repo);

        // Clear cache so it can update, including the settings used by webhooks.
        _cache.Remove(GetSettingsCacheKey(input.Owner, input.Repo));
        _cache.Remove(GetWebhookSettingsCacheKey(input.Owner, input.Repo));

        return new StatusResponse
        {
            Success = true
        };
    }

    /// <summary>Normalizes the environment for consistent processing.</summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>Text produced or selected by the operation.</returns>
    public string NormalizeEnvironment(string environment)
    {
        return NormalizeEnvironmentRegex.Replace(environment, string.Empty);
    }

    /// <summary>Performs the load default owner settings operation and returns its result.</summary>
    /// <returns>The deploy settings produced by the operation.</returns>
    private async Task<DeploySettings> LoadDefaultOwnerSettingsAsync()
    {
        _logger.LogDebug("Loading default owner settings from default-owner-settings.yaml.");
        var yaml = await File.ReadAllTextAsync("default-owner-settings.yaml");
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var deploySettings = deserializer.Deserialize<DeploySettings>(yaml);
        return deploySettings;
    }

    /// <summary>Retrieves the settings cache key.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    private static string GetSettingsCacheKey(string owner, string repo) => $"{owner}:{repo}:settings";

    /// <summary>Retrieves the webhook settings cache key.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public static string GetWebhookSettingsCacheKey(string owner, string repo) => $"{owner}:{repo}:webhook-settings";
}

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Models.Settings.Compare;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrDeploy.Api.Business.Services;
public class DeploySettingsService : IDeploySettingsService
{
    private static readonly TimeSpan RepoSettingsExpiration = TimeSpan.FromMinutes(5);
    private static readonly Regex NormalizeEnvironmentRegex = new Regex(@"\d+$", RegexOptions.Compiled);
    private readonly IGitHubClient _gitHubClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private readonly PrDeployOptions _prDeployOptions;

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public DeploySettingsService(IGitHubClient gitHubClient, IMemoryCache cache, 
        IOptions<PrDeployOptions> prDeployOptions, ILogger<DeploySettingsService> logger)
    {
        _prDeployOptions = prDeployOptions.Value;
        _gitHubClient = gitHubClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DeploySettingsCompare> GetAllAsync(RepoQueryInput input)
    {
        // Add validation.
        var ownerSettings = await GetOwnerSettingsAsync(_prDeployOptions.Owner);
        var repoSettings = await GetRepoSettingsAsync(input.Owner, input.Repo);

        var settingsCompare = Map.Compare(ownerSettings, repoSettings);
        return settingsCompare;
    }

    public async Task<DeploySettings> GetMergedAsync(string owner, string repo)
    {
        var repoKey = GetSettingsCacheKey(owner, repo);
        var repoSettings = _cache.Get<DeploySettings>(repoKey);
        if (repoSettings != null)
        {
            return repoSettings;
        }

        var ownerSettings = await GetOwnerSettingsAsync(_prDeployOptions.Owner);
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
        var environmentSettings = repoSettings.Environments
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

    private async Task<DeploySettings> GetRepoSettingsAsync(string owner, string repo)
    {
        // Repo specific settings, which are optional.
        DeploySettings deploySettings;
        try
        {
            deploySettings = await GetGenericSettingsAsync(owner, repo, _prDeployOptions.RepoSettingsPath, Deserializer);
        }
        catch
        {
            deploySettings = new();
            _logger.LogWarning($"The repository {owner}/{repo} does not yet have a {_prDeployOptions.RepoSettingsPath} file.");
        }

        return deploySettings;
    }

    private async Task<DeploySettings> GetOwnerSettingsAsync(string owner)
    {
        var defaultSettings = await GetGenericSettingsAsync(
            owner,
            _prDeployOptions.Repo,
            _prDeployOptions.DefaultSettingsPath, Deserializer);
        return defaultSettings;
    }

    public string NormalizeEnvironment(string environment)
    {
        return NormalizeEnvironmentRegex.Replace(environment, string.Empty);
    }

    private static string GetSettingsCacheKey(string owner, string repo) => $"{owner}:{repo}:settings";

    private async Task<DeploySettings> GetGenericSettingsAsync(string owner, string repo, string settingsPath,
        IDeserializer deserializer)
    {
        var settingsBytes = await _gitHubClient.Repository.Content.GetRawContent(owner, repo,
            settingsPath);
        var settingsYaml = Encoding.UTF8.GetString(settingsBytes, 0, settingsBytes.Length);
        var settings = deserializer.Deserialize<DeploySettings>(settingsYaml) ?? new();

        return settings;
    }
}

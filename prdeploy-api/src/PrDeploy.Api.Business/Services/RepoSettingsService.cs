using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using PrDeploy.Api.Business.Models.Settings;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrDeploy.Api.Business.Services;
public class RepoSettingsService : IRepoSettingsService
{
    private static readonly TimeSpan RepoSettingsExperation = TimeSpan.FromMinutes(5);
    private static readonly Regex NormalizeEnvironmentRegex = new Regex(@"\d+$", RegexOptions.Compiled);
    private readonly IGitHubClient _gitHubClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private readonly PrDeployOptions _prDeployOptions;

    public RepoSettingsService(IGitHubClient gitHubClient, IMemoryCache cache, IOptions<PrDeployOptions> prDeployOptions, ILogger<RepoSettingsService> logger)
    {
        _prDeployOptions = prDeployOptions.Value;
        _gitHubClient = gitHubClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RepoSettings> GetAsync(string owner, string repo)
    {
        var repoKey = GetSettingsCacheKey(owner, repo);
        var repoSettings = _cache.Get<RepoSettings>(repoKey);
        if (repoSettings != null)
        {
            return repoSettings;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        // Default settings.
        var defaultSettings = await GetGenericSettingsAsync(
            _prDeployOptions.Owner,
            _prDeployOptions.Repo,
            _prDeployOptions.DefaultSettingsPath, deserializer);

        // Repo specific settings, which are optional.
        try
        {
            repoSettings = await GetGenericSettingsAsync(owner, repo, _prDeployOptions.RepoSettingsPath, deserializer);
        }
        catch
        {
            repoSettings = new();
            _logger.LogWarning($"The repository {owner}/{repo} does not yet have a {_prDeployOptions.RepoSettingsPath} file.");
        }

        repoSettings.Owner = owner;
        repoSettings.Repo = repo;

        // Override default with repo.
        Map.Merge(repoSettings, defaultSettings);
        _cache.Set(repoKey, repoSettings, RepoSettingsExperation);

        return repoSettings;
    }

    public async Task<EnvironmentSettings> GetEnvironmentAsync(string owner, string repo, string environment)
    {
        var repoSettings = await GetAsync(owner, repo);
        var result = GetEnvironment(owner, repo, environment, repoSettings);
        return result;
    }

    public EnvironmentSettings? GetEnvironment(string owner, string repo, string environment,
        RepoSettings repoSettings)
    {
        var result = repoSettings.Environments
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
        var repoSettings = await GetAsync(owner, repo);
        var environmentSettings = repoSettings.Environments
            .GroupBy(e => e.Queue).Select(g =>
            {
                var environment = g.First();
                environment.Name = NormalizeEnvironment(environment.Name);
                return environment;
            }).ToList();

        return environmentSettings;
    }

    public string NormalizeEnvironment(string environment)
    {
        return NormalizeEnvironmentRegex.Replace(environment, string.Empty);
    }

    public async Task<List<string>> ListServicesAsync(string owner, string repo)
    {
        List<string>? services = null;

        var settings = await GetAsync(owner, repo);
        if (settings.Services?.Any() == true)
        {
            services = settings.Services.Select(s => s.Name).ToList();
        }

        return services ?? new List<string>();
    }

    private static string GetSettingsCacheKey(string owner, string repo) => $"{owner}:{repo}:settings";

    private async Task<RepoSettings> GetGenericSettingsAsync(string owner, string repo, string settingsPath,
        IDeserializer deserializer)
    {
        var settingsBytes = await _gitHubClient.Repository.Content.GetRawContent(owner, repo,
            settingsPath);
        var settingsYaml = Encoding.UTF8.GetString(settingsBytes, 0, settingsBytes.Length);
        var settings = deserializer.Deserialize<RepoSettings>(settingsYaml) ?? new();

        return settings;
    }
}

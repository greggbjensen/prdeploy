using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Business.Services;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Webhooks.Models;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>
/// Loads the merged owner and repository deploy settings for the repository that sent an event.
/// </summary>
/// <remarks>
/// Unlike <see cref="DeploySettingsService"/>, this does not guard on the signed in user, because events are
/// authenticated by the webhook signature and act as the GitHub App installation.
/// </remarks>
public class WebhookSettingsService
{
    private const string DeploySettingsKey = "DEPLOY_SETTINGS";
    private const string DefaultBranch = "main";
    private const string LockSuffix = "-lock";

    // Keep cache short so it can easily get latest.
    private static readonly TimeSpan SettingsCacheTimeout = TimeSpan.FromMinutes(5);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    private static readonly Dictionary<string, string> DefaultEnvironmentColors = new()
    {
        ["default"] = "#ededed",
        ["dev"] = "#d4ac0d",
        ["stage"] = "#2e86c1",
        ["prod"] = "#1d8348"
    };

    private readonly WebhookContext _context;
    private readonly IDeploymentStore _deploymentStore;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WebhookSettingsService> _logger;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <param name="deploymentStore">Object containing the deployment store data used by the operation.</param>
    /// <param name="cache">Object containing the cache data used by the operation.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public WebhookSettingsService(WebhookContext context, IDeploymentStore deploymentStore, IMemoryCache cache,
        ILogger<WebhookSettingsService> logger)
    {
        _context = context;
        _deploymentStore = deploymentStore;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the requested value.
    /// </summary>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public async Task<RepoSettings> GetAsync()
    {
        var cacheKey = DeploySettingsService.GetWebhookSettingsCacheKey(_context.Owner, _context.Repo);
        if (_cache.TryGetValue(cacheKey, out RepoSettings? settings) && settings != null)
        {
            return settings;
        }

        var settingsLock = Locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await settingsLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(cacheKey, out settings) && settings != null)
            {
                return settings;
            }

            var ownerSettings = await GetOwnerSettingsAsync();
            var deploySettings = await GetRepoSettingsAsync();

            // Repository settings override owner defaults.
            Map.Merge(deploySettings, ownerSettings);
            NormalizeDefaults(deploySettings);
            await ApplyEnvironmentColorsAsync(deploySettings);

            settings = new RepoSettings(deploySettings, await GetDefaultBranchAsync());
            _cache.Set(cacheKey, settings, SettingsCacheTimeout);

            return settings;
        }
        finally
        {
            settingsLock.Release();
        }
    }

    /// <summary>
    /// Normalizes the defaults for consistent processing.
    /// </summary>
    /// <param name="settings">Settings used to configure the operation.</param>
    internal static void NormalizeDefaults(DeploySettings settings)
    {
        settings.Services ??= [];
        foreach (var service in settings.Services)
        {
            // Default service to root path of same name.
            if (string.IsNullOrEmpty(service.Path))
            {
                service.Path = service.Name;
            }
        }

        settings.Environments ??= [];
        foreach (var environment in settings.Environments)
        {
            if (environment.AutomationTest == null)
            {
                environment.AutomationTest = new AutomationTestSettings { Enabled = false };
            }
            else if (environment.AutomationTest.Enabled == null)
            {
                // If there is a workflow and no enabled value is set, then enable it.
                environment.AutomationTest.Enabled = !string.IsNullOrEmpty(environment.AutomationTest.Workflow);
            }

            environment.AutomationTest.Inputs ??= [];
            environment.ExcludeFromRollback ??= [];
        }

        settings.Jira ??= new JiraSettings();
        settings.Slack ??= new SlackSettings();
        settings.Slack.Webhooks ??= new SlackWebHooksSettings();
        settings.Badge ??= new BadgeSettings();
        settings.Badge.StatusColors ??= new BadgeStatusColorsSettings();
    }

    /// <summary>
    /// Retrieves the owner settings.
    /// </summary>
    /// <returns>Requested value, or its default when no value is available.</returns>
    private async Task<DeploySettings?> GetOwnerSettingsAsync()
    {
        var settings = await _deploymentStore.GetAsync<DeploySettings?>(_context.Owner, DeploySettingsKey);
        if (settings == null)
        {
            _logger.LogError(
                "Unable to retrieve owner {Owner} settings, make sure to configure this in the portal.",
                _context.Owner);
        }

        return settings;
    }

    /// <summary>
    /// Retrieves the repo settings.
    /// </summary>
    /// <returns>Requested value, or its default when no value is available.</returns>
    private async Task<DeploySettings> GetRepoSettingsAsync()
    {
        var settings =
            await _deploymentStore.GetAsync<DeploySettings?>(_context.Owner, _context.Repo, DeploySettingsKey);
        if (settings == null)
        {
            _logger.LogWarning(
                "Repository {Owner}/{Repo} does not have repository specific settings, using defaults.",
                _context.Owner, _context.Repo);
            settings = new DeploySettings();
        }

        return settings;
    }

    /// <summary>
    /// Retrieves the default branch.
    /// </summary>
    /// <returns>Requested value, or its default when no value is available.</returns>
    private async Task<string> GetDefaultBranchAsync()
    {
        try
        {
            var repository = await _context.GitHub.Repository.Get(_context.Owner, _context.Repo);
            return repository.DefaultBranch ?? DefaultBranch;
        }
        catch (NotFoundException)
        {
            _logger.LogError("Repository {Owner}/{Repo} could not be found.", _context.Owner, _context.Repo);
            return DefaultBranch;
        }
    }

    /// <summary>
    /// Performs the apply environment colors operation.
    /// </summary>
    /// <param name="settings">Settings used to configure the operation.</param>
    private async Task ApplyEnvironmentColorsAsync(DeploySettings settings)
    {
        if (settings.Environments == null || settings.Environments.Count == 0)
        {
            return;
        }

        var labels = await _context.GitHub.Issue.Labels.GetAllForRepository(_context.Owner, _context.Repo);

        foreach (var environment in settings.Environments)
        {
            var lowerEnvironment = environment.Name.ToLowerInvariant();
            if (string.IsNullOrEmpty(environment.Color))
            {
                environment.Color = GetDefaultColor(environment.Name);
            }

            // Label colors do not include the hash for hex values.
            var color = environment.Color.TrimStart('#');
            var description = $"Pull request deployed to {lowerEnvironment} environment.";
            var environmentLabel = labels.FirstOrDefault(l =>
                string.Equals(l.Name, lowerEnvironment, StringComparison.OrdinalIgnoreCase));
            if (environmentLabel != null)
            {
                // Keep label and environment color in sync.
                if (!string.Equals(environmentLabel.Color, color, StringComparison.OrdinalIgnoreCase))
                {
                    await _context.GitHub.Issue.Labels.Update(_context.Owner, _context.Repo, environmentLabel.Name,
                        new LabelUpdate(lowerEnvironment, color) { Description = description });
                }
            }
            else
            {
                _logger.LogWarning("Label does not exist yet for {Label}, creating it.", lowerEnvironment);
                await _context.GitHub.Issue.Labels.Create(_context.Owner, _context.Repo,
                    new NewLabel(lowerEnvironment, color) { Description = description });
            }

            var environmentLock = $"{lowerEnvironment}{LockSuffix}";
            if (!labels.Any(l => string.Equals(l.Name, environmentLock, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Label does not exist yet for {Label}, creating it.", environmentLock);
                await _context.GitHub.Issue.Labels.Create(_context.Owner, _context.Repo,
                    new NewLabel(environmentLock, GetDefaultColor(environmentLock).TrimStart('#'))
                    {
                        Description = $"Environment {lowerEnvironment} is locked for use by pull request."
                    });
            }
        }
    }

    /// <summary>
    /// Retrieves the default color.
    /// </summary>
    /// <param name="label">Text used as the label.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    private static string GetDefaultColor(string label)
    {
        if (label.EndsWith(LockSuffix, StringComparison.Ordinal))
        {
            return DefaultEnvironmentColors["default"];
        }

        if (label.StartsWith("dev", StringComparison.Ordinal))
        {
            return DefaultEnvironmentColors["dev"];
        }

        if (label.StartsWith("stag", StringComparison.Ordinal))
        {
            return DefaultEnvironmentColors["stage"];
        }

        return label.StartsWith("prod", StringComparison.Ordinal)
            ? DefaultEnvironmentColors["prod"]
            : DefaultEnvironmentColors["default"];
    }
}

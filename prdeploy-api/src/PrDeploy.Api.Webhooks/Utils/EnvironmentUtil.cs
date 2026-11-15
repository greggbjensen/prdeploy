using System.Text.RegularExpressions;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Webhooks.Utils;

public partial class EnvironmentUtil
{
    private readonly WebhookContext _context;

    public EnvironmentUtil(WebhookContext context)
    {
        _context = context;
    }

    public EnvironmentSettings? GetSettings(string? environment)
    {
        if (string.IsNullOrEmpty(environment))
        {
            return null;
        }

        return _context.Settings.Environments?
            .Find(e => string.Equals(e.Name, environment, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the settings for an environment that is expected to be configured.
    /// </summary>
    public EnvironmentSettings GetRequiredSettings(string environment)
    {
        return GetSettings(environment)
               ?? throw new InvalidOperationException(
                   $"Environment {environment} is not configured for {_context.Owner}/{_context.Repo}.");
    }

    /// <summary>
    /// Removes any ending number and lock suffix, so dev2-lock becomes dev.
    /// </summary>
    public string Normalize(string? environment)
    {
        var normalEnvironment = !string.IsNullOrEmpty(environment)
            ? environment.ToLowerInvariant()
            : _context.Settings.DefaultEnvironment?.ToLowerInvariant() ?? string.Empty;

        return NormalizeEnvironmentRegex().Replace(normalEnvironment, string.Empty);
    }

    public string GetFromLock(string lockLabel)
    {
        return EnvironmentLockRegex().Replace(lockLabel.ToLowerInvariant(), string.Empty);
    }

    [GeneratedRegex(@"(\d+)?(-lock)?$", RegexOptions.IgnoreCase)]
    private static partial Regex NormalizeEnvironmentRegex();

    [GeneratedRegex(@"(-lock)$", RegexOptions.IgnoreCase)]
    private static partial Regex EnvironmentLockRegex();
}

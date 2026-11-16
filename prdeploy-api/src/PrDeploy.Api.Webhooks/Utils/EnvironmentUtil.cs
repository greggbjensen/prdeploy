using System.Text.RegularExpressions;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Webhooks.Utils;

/// <summary>
/// Represents the environment Util used by the application.
/// </summary>
public partial class EnvironmentUtil
{
    private readonly WebhookContext _context;

    /// <summary>
    /// Initializes a environment Util with the supplied dependencies.
    /// </summary>
    /// <param name="context">Request or application context used by the operation.</param>
    public EnvironmentUtil(WebhookContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves the Settings.
    /// </summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>The value produced by this operation.</returns>
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

    /// <summary>
    /// Retrieves the From Lock.
    /// </summary>
    /// <param name="lockLabel">Value supplied for lock Label.</param>
    /// <returns>The value produced by this operation.</returns>
    public string GetFromLock(string lockLabel)
    {
        return EnvironmentLockRegex().Replace(lockLabel.ToLowerInvariant(), string.Empty);
    }

    /// <summary>
    /// Normalizes the Environment Regex for consistent processing.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"(\d+)?(-lock)?$", RegexOptions.IgnoreCase)]
    private static partial Regex NormalizeEnvironmentRegex();

    /// <summary>
    /// Performs the environment Lock Regex operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    [GeneratedRegex(@"(-lock)$", RegexOptions.IgnoreCase)]
    private static partial Regex EnvironmentLockRegex();
}

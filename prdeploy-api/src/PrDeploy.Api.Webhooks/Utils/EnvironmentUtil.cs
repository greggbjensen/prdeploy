using System.Text.RegularExpressions;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Webhooks.Utils;

/// <summary>Represents environment util data used by the application.</summary>
public partial class EnvironmentUtil
{
    private readonly WebhookContext _context;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="context">Request or application context used by the operation.</param>
    public EnvironmentUtil(WebhookContext context)
    {
        _context = context;
    }

    /// <summary>Retrieves the settings.</summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public EnvironmentSettings? GetSettings(string? environment)
    {
        if (string.IsNullOrEmpty(environment))
        {
            return null;
        }

        return _context.Settings.Environments?
            .Find(e => string.Equals(e.Name, environment, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Retrieves the required settings.</summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public EnvironmentSettings GetRequiredSettings(string environment)
    {
        return GetSettings(environment)
               ?? throw new InvalidOperationException(
                   $"Environment {environment} is not configured for {_context.Owner}/{_context.Repo}.");
    }

    /// <summary>Removes any ending number and lock suffix, so dev2-lock becomes dev.</summary>
    public string Normalize(string? environment)
    {
        var normalEnvironment = !string.IsNullOrEmpty(environment)
            ? environment.ToLowerInvariant()
            : _context.Settings.DefaultEnvironment?.ToLowerInvariant() ?? string.Empty;

        return NormalizeEnvironmentRegex().Replace(normalEnvironment, string.Empty);
    }

    /// <summary>Retrieves the from lock.</summary>
    /// <param name="lockLabel">Text used as the lock label.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public string GetFromLock(string lockLabel)
    {
        return EnvironmentLockRegex().Replace(lockLabel.ToLowerInvariant(), string.Empty);
    }

    /// <summary>Normalizes the environment regex for consistent processing.</summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"(\d+)?(-lock)?$", RegexOptions.IgnoreCase)]
    private static partial Regex NormalizeEnvironmentRegex();

    /// <summary>Performs the environment lock regex operation and returns its result.</summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"(-lock)$", RegexOptions.IgnoreCase)]
    private static partial Regex EnvironmentLockRegex();
}

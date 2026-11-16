namespace PrDeploy.Api.Models.Settings.Compare;

/// <summary>
/// Represents the slack Web Hooks Settings Compare used by the application.
/// </summary>
public class SlackWebHooksSettingsCompare
{
    /// <summary>
    /// Gets or sets the Deploy Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<string> DeployUrl { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Release Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<string> ReleaseUrl { get; set; } = null!;
}

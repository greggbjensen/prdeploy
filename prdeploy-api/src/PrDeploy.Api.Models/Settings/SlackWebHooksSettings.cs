namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for the slack Web Hooks Settings.
/// </summary>
public class SlackWebHooksSettings
{
    /// <summary>
    /// Gets or sets the Deploy Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? DeployUrl { get; set; }
    /// <summary>
    /// Gets or sets the Release Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? ReleaseUrl { get; set; }
}

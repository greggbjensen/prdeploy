namespace PrDeploy.Api.Models.Settings;
/// <summary>Stores configuration values for Microsoft Teams web hooks.</summary>
public class TeamsWebHooksSettings
{
    /// <summary>Gets or sets the deploy URL.</summary>
    /// <value>Current deploy URL value.</value>
    public string? DeployUrl { get; set; }
    /// <summary>Gets or sets the release URL.</summary>
    /// <value>Current release URL value.</value>
    public string? ReleaseUrl { get; set; }
}

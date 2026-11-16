namespace PrDeploy.Api.Models.Settings.Compare;

/// <summary>
/// Represents Slack web hooks settings compare data used by the application.
/// </summary>
public class SlackWebHooksSettingsCompare
{
    /// <summary>
    /// Gets or sets the deploy URL.
    /// </summary>
    /// <value>Current deploy URL value.</value>
    public OwnerRepoValue<string> DeployUrl { get; set; } = null!;
    /// <summary>
    /// Gets or sets the release URL.
    /// </summary>
    /// <value>Current release URL value.</value>
    public OwnerRepoValue<string> ReleaseUrl { get; set; } = null!;
}

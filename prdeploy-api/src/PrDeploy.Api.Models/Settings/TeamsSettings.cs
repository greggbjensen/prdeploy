namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for Microsoft Teams.
/// </summary>
public class TeamsSettings
{
    /// <summary>
    /// Gets or sets the webhooks.
    /// </summary>
    /// <value>Current webhooks value.</value>
    public TeamsWebHooksSettings? Webhooks { get; set; }
    /// <summary>
    /// Gets or sets the notifications enabled.
    /// </summary>
    /// <value>Current notifications enabled value.</value>
    public bool? NotificationsEnabled { get; set; }
}

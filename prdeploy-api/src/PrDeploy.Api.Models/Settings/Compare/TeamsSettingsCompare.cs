namespace PrDeploy.Api.Models.Settings.Compare;
/// <summary>
/// Represents Microsoft Teams settings compare data used by the application.
/// </summary>
public class TeamsSettingsCompare
{
    /// <summary>
    /// Gets or sets the webhooks.
    /// </summary>
    /// <value>Current webhooks value.</value>
    public TeamsWebHooksSettingsCompare Webhooks { get; set; } = null!;
    /// <summary>
    /// Gets or sets the notifications enabled.
    /// </summary>
    /// <value>Current notifications enabled value.</value>
    public OwnerRepoValue<bool?> NotificationsEnabled { get; set; } = null!;
}

namespace PrDeploy.Api.Models.Settings.Compare;
/// <summary>
/// Represents Slack settings compare data used by the application.
/// </summary>
public class SlackSettingsCompare
{
    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    /// <value>Current token value.</value>
    public OwnerRepoValue<string> Token { get; set; } = null!;
    /// <summary>
    /// Gets or sets the email domain.
    /// </summary>
    /// <value>Current email domain value.</value>
    public OwnerRepoValue<string> EmailDomain { get; set; } = null!;
    /// <summary>
    /// Gets or sets the email aliases.
    /// </summary>
    /// <value>Current email aliases value.</value>
    public OwnerRepoDictionary EmailAliases { get; set; } = null!;
    /// <summary>
    /// Gets or sets the webhooks.
    /// </summary>
    /// <value>Current webhooks value.</value>
    public SlackWebHooksSettingsCompare Webhooks { get; set; } = null!;
    /// <summary>
    /// Gets or sets the notifications enabled.
    /// </summary>
    /// <value>Current notifications enabled value.</value>
    public OwnerRepoValue<bool?> NotificationsEnabled { get; set; } = null!;
}

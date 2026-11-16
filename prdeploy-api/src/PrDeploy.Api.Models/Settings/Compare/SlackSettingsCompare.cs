namespace PrDeploy.Api.Models.Settings.Compare;
/// <summary>
/// Represents the slack Settings Compare used by the application.
/// </summary>
public class SlackSettingsCompare
{
    /// <summary>
    /// Gets or sets the Token.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<string> Token { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Email Domain.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<string> EmailDomain { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Email Aliases.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoDictionary EmailAliases { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Webhooks.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public SlackWebHooksSettingsCompare Webhooks { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Notifications Enabled.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<bool?> NotificationsEnabled { get; set; } = null!;
}

namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for badge.
/// </summary>
public class BadgeSettings
{
    /// <summary>
    /// Gets or sets the status colors.
    /// </summary>
    /// <value>Current status colors value.</value>
    public BadgeStatusColorsSettings? StatusColors { get; set; }
}

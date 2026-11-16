namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for the badge Settings.
/// </summary>
public class BadgeSettings
{
    /// <summary>
    /// Gets or sets the Status Colors.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public BadgeStatusColorsSettings? StatusColors { get; set; }
}

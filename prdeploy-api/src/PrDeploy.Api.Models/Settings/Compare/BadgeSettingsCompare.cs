namespace PrDeploy.Api.Models.Settings.Compare;
/// <summary>
/// Represents the badge Settings Compare used by the application.
/// </summary>
public class BadgeSettingsCompare
{
    /// <summary>
    /// Gets or sets the Status Colors.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public BadgeStatusColorsSettingsCompare StatusColors { get; set; } = null!;
}

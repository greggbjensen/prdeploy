namespace PrDeploy.Api.Models.Settings.Compare;
/// <summary>Represents badge settings compare data used by the application.</summary>
public class BadgeSettingsCompare
{
    /// <summary>Gets or sets the status colors.</summary>
    /// <value>Current status colors value.</value>
    public BadgeStatusColorsSettingsCompare StatusColors { get; set; } = null!;
}

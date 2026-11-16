namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for the badge Status Colors Settings.
/// </summary>
public class BadgeStatusColorsSettings
{
    /// <summary>
    /// Gets or sets the Error.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets the Warn.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Warn { get; set; }
    /// <summary>
    /// Gets or sets the Success.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Success { get; set; }
    /// <summary>
    /// Gets or sets the Info.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Info { get; set; }
}

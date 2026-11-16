namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for badge status colors.
/// </summary>
public class BadgeStatusColorsSettings
{
    /// <summary>
    /// Gets or sets the error.
    /// </summary>
    /// <value>Current error value.</value>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets the warn.
    /// </summary>
    /// <value>Current warn value.</value>
    public string? Warn { get; set; }
    /// <summary>
    /// Gets or sets the success.
    /// </summary>
    /// <value>Current success value.</value>
    public string? Success { get; set; }
    /// <summary>
    /// Gets or sets the info.
    /// </summary>
    /// <value>Current info value.</value>
    public string? Info { get; set; }
}

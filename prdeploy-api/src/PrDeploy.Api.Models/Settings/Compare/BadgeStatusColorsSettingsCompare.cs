namespace PrDeploy.Api.Models.Settings.Compare;
/// <summary>
/// Represents the badge Status Colors Settings Compare used by the application.
/// </summary>
public class BadgeStatusColorsSettingsCompare
{
    /// <summary>
    /// Gets or sets the Error.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<string> Error { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Warn.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<string> Warn { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Success.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<string> Success { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Info.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<string> Info { get; set; } = null!;
}

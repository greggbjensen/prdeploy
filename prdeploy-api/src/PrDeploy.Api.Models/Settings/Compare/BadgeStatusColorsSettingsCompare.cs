namespace PrDeploy.Api.Models.Settings.Compare;
/// <summary>Represents badge status colors settings compare data used by the application.</summary>
public class BadgeStatusColorsSettingsCompare
{
    /// <summary>Gets or sets the error.</summary>
    /// <value>Current error value.</value>
    public OwnerRepoValue<string> Error { get; set; } = null!;
    /// <summary>Gets or sets the warn.</summary>
    /// <value>Current warn value.</value>
    public OwnerRepoValue<string> Warn { get; set; } = null!;
    /// <summary>Gets or sets the success.</summary>
    /// <value>Current success value.</value>
    public OwnerRepoValue<string> Success { get; set; } = null!;
    /// <summary>Gets or sets the info.</summary>
    /// <value>Current info value.</value>
    public OwnerRepoValue<string> Info { get; set; } = null!;
}

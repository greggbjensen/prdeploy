namespace PrDeploy.Api.Models.Settings.Compare;
/// <summary>
/// Represents builds settings compare data used by the application.
/// </summary>
public class BuildsSettingsCompare
{
    /// <summary>
    /// Gets or sets the check pattern.
    /// </summary>
    /// <value>Current check pattern value.</value>
    public OwnerRepoValue<string> CheckPattern { get; set; } = null!;
}

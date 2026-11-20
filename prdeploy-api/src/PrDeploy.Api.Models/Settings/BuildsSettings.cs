namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for builds.
/// </summary>
public class BuildsSettings
{
    /// <summary>
    /// Gets or sets the check pattern.
    /// </summary>
    /// <value>Current check pattern value.</value>
    public string? CheckPattern { get; set; }
}

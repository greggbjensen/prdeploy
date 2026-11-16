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
    /// <summary>
    /// Gets or sets the workflow pattern.
    /// </summary>
    /// <value>Current workflow pattern value.</value>
    public string? WorkflowPattern { get; set; }
}

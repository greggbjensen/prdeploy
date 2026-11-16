namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for the builds Settings.
/// </summary>
public class BuildsSettings
{
    /// <summary>
    /// Gets or sets the Check Pattern.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? CheckPattern { get; set; }    
    /// <summary>
    /// Gets or sets the Workflow Pattern.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? WorkflowPattern { get; set; }
}

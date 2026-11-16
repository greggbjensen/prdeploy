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
    /// <summary>
    /// Gets or sets the workflow pattern.
    /// </summary>
    /// <value>Current workflow pattern value.</value>
    public OwnerRepoValue<string> WorkflowPattern { get; set; } = null!;
}

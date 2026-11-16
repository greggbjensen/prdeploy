namespace PrDeploy.Api.Models.Settings.Compare;
/// <summary>
/// Represents the builds Settings Compare used by the application.
/// </summary>
public class BuildsSettingsCompare
{
    /// <summary>
    /// Gets or sets the Check Pattern.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<string> CheckPattern { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Workflow Pattern.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public OwnerRepoValue<string> WorkflowPattern { get; set; } = null!;
}

namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for the environment Settings.
/// </summary>
public class EnvironmentSettings
{
    /// <summary>
    /// Gets or sets the Name.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Queue.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Queue { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Color.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Color { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Url { get; set; }
    /// <summary>
    /// Gets or sets the Require Approval.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public bool RequireApproval { get; set; }
    /// <summary>
    /// Gets or sets the Require Branch Up To Date.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public bool RequireBranchUpToDate { get; set; }
    /// <summary>
    /// Gets or sets the Automation Test.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public AutomationTestSettings? AutomationTest { get; set; }
    /// <summary>
    /// Gets or sets the Exclude From Rollback.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public List<string>? ExcludeFromRollback { get; set; }
}

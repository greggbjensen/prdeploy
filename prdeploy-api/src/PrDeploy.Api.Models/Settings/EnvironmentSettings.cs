namespace PrDeploy.Api.Models.Settings;
/// <summary>Stores configuration values for environment.</summary>
public class EnvironmentSettings
{
    /// <summary>Gets or sets the name.</summary>
    /// <value>Current name value.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the queue.</summary>
    /// <value>Current queue value.</value>
    public string Queue { get; set; } = string.Empty;
    /// <summary>Gets or sets the color.</summary>
    /// <value>Current color value.</value>
    public string Color { get; set; } = string.Empty;
    /// <summary>Gets or sets the URL.</summary>
    /// <value>Current URL value.</value>
    /// <remarks>Legacy single URL, superseded by <see cref="Urls"/>; still read when <see cref="Urls"/> is empty.</remarks>
    public string? Url { get; set; }

    /// <summary>Gets or sets the labelled URLs shown for the environment.</summary>
    /// <value>Current URLs value.</value>
    public List<EnvironmentUrl>? Urls { get; set; }
    /// <summary>Gets or sets the require approval.</summary>
    /// <value>Current require approval value.</value>
    public bool RequireApproval { get; set; }
    /// <summary>Gets or sets the require branch up to date.</summary>
    /// <value>Current require branch up to date value.</value>
    public bool RequireBranchUpToDate { get; set; }
    /// <summary>Gets or sets the automation test.</summary>
    /// <value>Current automation test value.</value>
    public AutomationTestSettings? AutomationTest { get; set; }
    /// <summary>Gets or sets the exclude from rollback.</summary>
    /// <value>Current exclude from rollback value.</value>
    public List<string>? ExcludeFromRollback { get; set; }
}

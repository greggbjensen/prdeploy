namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Represents build data used by the application.
/// </summary>
public class Build
{
    /// <summary>
    /// Gets or sets the run id.
    /// </summary>
    /// <value>Current run id value.</value>
    public long RunId { get; set; }
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>Current name value.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    /// <value>Current version value.</value>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    /// <value>Current title value.</value>
    public string? Title { get; set; }
    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    /// <value>Current URL value.</value>
    public string? Url { get; set; }
}

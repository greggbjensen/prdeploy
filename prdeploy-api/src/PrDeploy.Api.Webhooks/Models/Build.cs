namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Represents the build used by the application.
/// </summary>
public class Build
{
    /// <summary>
    /// Gets or sets the Run Id.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public long RunId { get; set; }
    /// <summary>
    /// Gets or sets the Name.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Version.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Title.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Title { get; set; }
    /// <summary>
    /// Gets or sets the Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Url { get; set; }
}

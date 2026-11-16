namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Represents the jira Issue used by the application.
/// </summary>
public class JiraIssue
{
    /// <summary>
    /// Gets or sets the Key.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Summary.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Type.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Type { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Url { get; set; } = string.Empty;
}

namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Represents Jira issue data used by the application.
/// </summary>
public class JiraIssue
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>Current key value.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the summary.
    /// </summary>
    /// <value>Current summary value.</value>
    public string Summary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    /// <value>Current type value.</value>
    public string Type { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    /// <value>Current URL value.</value>
    public string Url { get; set; } = string.Empty;
}

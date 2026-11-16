namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Response from PUT /repos/{owner}/{repo}/pulls/{pull_number}/update-branch, which Octokit.NET does not wrap.
/// </summary>
public class UpdateBranchResponse
{
    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    /// <value>Current message value.</value>
    public string? Message { get; set; }
    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    /// <value>Current URL value.</value>
    public string? Url { get; set; }
}

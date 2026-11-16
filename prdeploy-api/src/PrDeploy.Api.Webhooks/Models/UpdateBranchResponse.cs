namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Response from PUT /repos/{owner}/{repo}/pulls/{pull_number}/update-branch, which Octokit.NET does not wrap.
/// </summary>
public class UpdateBranchResponse
{
    /// <summary>
    /// Gets or sets the Message.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Message { get; set; }
    /// <summary>
    /// Gets or sets the Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Url { get; set; }
}

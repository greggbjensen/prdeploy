namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Response from PUT /repos/{owner}/{repo}/pulls/{pull_number}/update-branch, which Octokit.NET does not wrap.
/// </summary>
public class UpdateBranchResponse
{
    public string? Message { get; set; }
    public string? Url { get; set; }
}

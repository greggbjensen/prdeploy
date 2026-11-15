namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Contents of build-details.json uploaded as an artifact by the build-details action.
/// </summary>
public class BuildDetails
{
    public long RunId { get; set; }
    public string? Version { get; set; }
    public string? Head { get; set; }
    public string? Base { get; set; }
    public string? Commit { get; set; }
    public List<string>? Issues { get; set; }
    public int PullRequest { get; set; }
    public string? Owner { get; set; }
    public string? Repo { get; set; }
    public string? EventName { get; set; }
}

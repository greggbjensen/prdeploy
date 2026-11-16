namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Contents of build-details.json uploaded as an artifact by the build-details action.
/// </summary>
public class BuildDetails
{
    /// <summary>
    /// Gets or sets the run id.
    /// </summary>
    /// <value>Current run id value.</value>
    public long RunId { get; set; }
    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    /// <value>Current version value.</value>
    public string? Version { get; set; }
    /// <summary>
    /// Gets or sets the head.
    /// </summary>
    /// <value>Current head value.</value>
    public string? Head { get; set; }
    /// <summary>
    /// Gets or sets the base.
    /// </summary>
    /// <value>Current base value.</value>
    public string? Base { get; set; }
    /// <summary>
    /// Gets or sets the commit.
    /// </summary>
    /// <value>Current commit value.</value>
    public string? Commit { get; set; }
    /// <summary>
    /// Gets or sets the issues.
    /// </summary>
    /// <value>Current issues value.</value>
    public List<string>? Issues { get; set; }
    /// <summary>
    /// Gets or sets the pull request.
    /// </summary>
    /// <value>Current pull request value.</value>
    public int PullRequest { get; set; }
    /// <summary>
    /// Gets or sets the owner.
    /// </summary>
    /// <value>Current owner value.</value>
    public string? Owner { get; set; }
    /// <summary>
    /// Gets or sets the repo.
    /// </summary>
    /// <value>Current repo value.</value>
    public string? Repo { get; set; }
    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    /// <value>Current event name value.</value>
    public string? EventName { get; set; }
}

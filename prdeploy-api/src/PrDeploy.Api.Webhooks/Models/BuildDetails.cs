namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Contents of build-details.json uploaded as an artifact by the build-details action.
/// </summary>
public class BuildDetails
{
    /// <summary>
    /// Gets or sets the Run Id.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public long RunId { get; set; }
    /// <summary>
    /// Gets or sets the Version.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Version { get; set; }
    /// <summary>
    /// Gets or sets the Head.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Head { get; set; }
    /// <summary>
    /// Gets or sets the Base.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Base { get; set; }
    /// <summary>
    /// Gets or sets the Commit.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Commit { get; set; }
    /// <summary>
    /// Gets or sets the Issues.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public List<string>? Issues { get; set; }
    /// <summary>
    /// Gets or sets the Pull Request.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public int PullRequest { get; set; }
    /// <summary>
    /// Gets or sets the Owner.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Owner { get; set; }
    /// <summary>
    /// Gets or sets the Repo.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Repo { get; set; }
    /// <summary>
    /// Gets or sets the Event Name.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? EventName { get; set; }
}

using Octokit;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Webhooks;

/// <summary>
/// Per-event state shared by webhook services, populated by the dispatcher before any manager runs.
/// </summary>
/// <remarks>
/// The GitHub client here is authenticated as the GitHub App installation, unlike the scoped
/// <see cref="IGitHubClient"/> used by the GraphQL API, which is authenticated as the signed in user.
/// </remarks>
public class WebhookContext
{
    /// <summary>
    /// Gets or sets the git hub.
    /// </summary>
    /// <value>Current git hub value.</value>
    public IGitHubClient GitHub { get; set; } = null!;
    /// <summary>
    /// Gets or sets the owner.
    /// </summary>
    /// <value>Current owner value.</value>
    public string Owner { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the repo.
    /// </summary>
    /// <value>Current repo value.</value>
    public string Repo { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the repository HTML URL.
    /// </summary>
    /// <value>Current repository HTML URL value.</value>
    public string RepositoryHtmlUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the default branch.
    /// </summary>
    /// <value>Current default branch value.</value>
    public string DefaultBranch { get; set; } = "main";
    /// <summary>
    /// Gets or sets the settings.
    /// </summary>
    /// <value>Current settings value.</value>
    public DeploySettings Settings { get; set; } = new();
}

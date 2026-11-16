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
    /// Gets or sets the Git Hub.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public IGitHubClient GitHub { get; set; } = null!;
    /// <summary>
    /// Gets or sets the Owner.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Owner { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Repo.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Repo { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Repository Html Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string RepositoryHtmlUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Default Branch.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string DefaultBranch { get; set; } = "main";
    /// <summary>
    /// Gets or sets the Settings.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public DeploySettings Settings { get; set; } = new();
}

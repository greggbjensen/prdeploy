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
    public IGitHubClient GitHub { get; set; } = null!;
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string RepositoryHtmlUrl { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
    public DeploySettings Settings { get; set; } = new();
}

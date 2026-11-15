namespace PrDeploy.Api.Models.Settings;
public class DeploySettings
{
    //// Shared by the GraphQL API and the GitHub App webhooks in PrDeploy.Api.Webhooks.
    public string? DeployWorkflow { get; set; }
    public List<EnvironmentSettings>? Environments { get; set; } = new ();
    public List<ServiceSettings>? Services { get; set; } = new();
    public string? DefaultEnvironment { get; set; }
    public string? ReleaseEnvironment { get; set; }
    public JiraSettings? Jira { get; set; }
    public BuildsSettings? Builds { get; set; }
    public SlackSettings? Slack { get; set; }
    public string? PrdeployPortalUrl { get; set; }
    public BadgeSettings? Badge { get; set; }
}

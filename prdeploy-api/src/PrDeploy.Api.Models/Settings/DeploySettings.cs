namespace PrDeploy.Api.Models.Settings;
public class DeploySettings
{
    //// This has to match prdeploy-webhooks/src/models/settings/repo-settings.ts.
    public string? Owner { get; set; }
    public string? Repo { get; set; }
    public string? DeployWorkflow { get; set; }
    public List<EnvironmentSettings>? Environments { get; set; } = new ();
    public List<ServiceSettings>? Services { get; set; } = new();
    public string? DefaultEnvironment { get; set; }
    public string? ReleaseEnvironment { get; set; }
    public string? DefaultBranch { get; set; }
    public string? SettingsBranch { get; set; }
    public JiraSettings? Jira { get; set; }
    public BuildsSettings? Builds { get; set; }
    public SlackSettings? Slack { get; set; }
    public string? DeployManagerSiteUrl { get; set; }
    public BadgeSettings? Badge { get; set; }
}

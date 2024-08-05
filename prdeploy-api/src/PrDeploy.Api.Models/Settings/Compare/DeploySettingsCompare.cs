namespace PrDeploy.Api.Models.Settings.Compare
{
    public class DeploySettingsCompare
    {
        public string? Owner { get; set; }
        public string? Repo { get; set; }
        public OwnerRepoValue<string> DeployWorkflow { get; set; }
        //public List<EnvironmentSettings>? Environments { get; set; } = new();
        //public List<ServiceSettings>? Services { get; set; } = new();
        //public string? DefaultEnvironment { get; set; }
        //public string? ReleaseEnvironment { get; set; }
        //public string? DefaultBranch { get; set; }
        //public string? SettingsBranch { get; set; }
        //public JiraSettings? Jira { get; set; }
        //public BuildsSettings? Builds { get; set; }
        //public SlackSettings? Slack { get; set; }
        //public string? DeployManagerSiteUrl { get; set; }
        //public BadgeSettings? Badge { get; set; }
    }
}

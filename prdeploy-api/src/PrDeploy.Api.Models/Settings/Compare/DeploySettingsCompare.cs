namespace PrDeploy.Api.Models.Settings.Compare
{
    public class DeploySettingsCompare
    {
        public OwnerRepoValue<string> DeployWorkflow { get; set; } = null!;
        public OwnerRepoValue<List<EnvironmentSettings>> Environments { get; set; } = null!;
        //public List<ServiceSettings>? Services { get; set; } = new();
        public OwnerRepoValue<string> DefaultEnvironment { get; set; } = null!;
        public OwnerRepoValue<string> ReleaseEnvironment { get; set; } = null!;
        public OwnerRepoValue<string> DefaultBranch { get; set; } = null!;
        public OwnerRepoValue<string> SettingsBranch { get; set; } = null!;
        //public JiraSettings? Jira { get; set; }
        public BuildsSettingsCompare Builds { get; set; } = null!;
        //public SlackSettings? Slack { get; set; }
        public OwnerRepoValue<string> DeployManagerSiteUrl { get; set; } = null!;
        //public BadgeSettings? Badge { get; set; }
    }
}

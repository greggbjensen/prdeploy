namespace PrDeploy.Api.Models.Settings.Compare
{
    public class DeploySettingsCompare
    {
        public OwnerRepoValue<string> DeployWorkflow { get; set; } = null!;
        public OwnerRepoValue<List<EnvironmentSettings>> Environments { get; set; } = null!;
        public OwnerRepoValue<List<ServiceSettings>> Services { get; set; } = new();
        public OwnerRepoValue<string> DefaultEnvironment { get; set; } = null!;
        public OwnerRepoValue<string> ReleaseEnvironment { get; set; } = null!;
        public JiraSettingsCompare Jira { get; set; } = null!;
        public BuildsSettingsCompare Builds { get; set; } = null!;
        public SlackSettingsCompare Slack { get; set; } = null!;
        public OwnerRepoValue<string> PrdeployPortalUrl { get; set; } = null!;
        public BadgeSettingsCompare Badge { get; set; } = null!;
    }
}

namespace PrDeploy.Api.Models.Settings.Compare
{
    /// <summary>
    /// Represents deploy settings compare data used by the application.
    /// </summary>
    public class DeploySettingsCompare
    {
        /// <summary>
        /// Gets or sets the deploy workflow.
        /// </summary>
        /// <value>Current deploy workflow value.</value>
        public OwnerRepoValue<string> DeployWorkflow { get; set; } = null!;
        /// <summary>
        /// Gets or sets the environments.
        /// </summary>
        /// <value>Current environments value.</value>
        public OwnerRepoValue<List<EnvironmentSettings>> Environments { get; set; } = null!;
        /// <summary>
        /// Gets or sets the services.
        /// </summary>
        /// <value>Current services value.</value>
        public OwnerRepoValue<List<ServiceSettings>> Services { get; set; } = new();
        /// <summary>
        /// Gets or sets the default environment.
        /// </summary>
        /// <value>Current default environment value.</value>
        public OwnerRepoValue<string> DefaultEnvironment { get; set; } = null!;
        /// <summary>
        /// Gets or sets the release environment.
        /// </summary>
        /// <value>Current release environment value.</value>
        public OwnerRepoValue<string> ReleaseEnvironment { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Jira.
        /// </summary>
        /// <value>Current Jira value.</value>
        public JiraSettingsCompare Jira { get; set; } = null!;
        /// <summary>
        /// Gets or sets the builds.
        /// </summary>
        /// <value>Current builds value.</value>
        public BuildsSettingsCompare Builds { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Slack.
        /// </summary>
        /// <value>Current Slack value.</value>
        public SlackSettingsCompare Slack { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Microsoft Teams.
        /// </summary>
        /// <value>Current Microsoft Teams value.</value>
        public TeamsSettingsCompare Teams { get; set; } = null!;
        /// <summary>
        /// Gets or sets the PrDeploy portal URL.
        /// </summary>
        /// <value>Current PrDeploy portal URL value.</value>
        public OwnerRepoValue<string> PrdeployPortalUrl { get; set; } = null!;
        /// <summary>
        /// Gets or sets the badge.
        /// </summary>
        /// <value>Current badge value.</value>
        public BadgeSettingsCompare Badge { get; set; } = null!;
    }
}

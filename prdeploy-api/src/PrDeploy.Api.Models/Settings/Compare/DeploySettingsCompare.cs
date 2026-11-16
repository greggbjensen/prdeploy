namespace PrDeploy.Api.Models.Settings.Compare
{
    /// <summary>
    /// Represents the deploy Settings Compare used by the application.
    /// </summary>
    public class DeploySettingsCompare
    {
        /// <summary>
        /// Gets or sets the Deploy Workflow.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public OwnerRepoValue<string> DeployWorkflow { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Environments.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public OwnerRepoValue<List<EnvironmentSettings>> Environments { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Services.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public OwnerRepoValue<List<ServiceSettings>> Services { get; set; } = new();
        /// <summary>
        /// Gets or sets the Default Environment.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public OwnerRepoValue<string> DefaultEnvironment { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Release Environment.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public OwnerRepoValue<string> ReleaseEnvironment { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Jira.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public JiraSettingsCompare Jira { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Builds.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public BuildsSettingsCompare Builds { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Slack.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public SlackSettingsCompare Slack { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Prdeploy Portal Url.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public OwnerRepoValue<string> PrdeployPortalUrl { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Badge.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public BadgeSettingsCompare Badge { get; set; } = null!;
    }
}

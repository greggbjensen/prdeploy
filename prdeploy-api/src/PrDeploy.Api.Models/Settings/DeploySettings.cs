namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for deploy.
/// </summary>
public class DeploySettings
{
    //// Shared by the GraphQL API and the GitHub App webhooks in PrDeploy.Api.Webhooks.
    /// <summary>
    /// Gets or sets the deploy workflow.
    /// </summary>
    /// <value>Current deploy workflow value.</value>
    public string? DeployWorkflow { get; set; }
    /// <summary>
    /// Gets or sets the environments.
    /// </summary>
    /// <value>Current environments value.</value>
    public List<EnvironmentSettings>? Environments { get; set; } = new ();
    /// <summary>
    /// Gets or sets the services.
    /// </summary>
    /// <value>Current services value.</value>
    public List<ServiceSettings>? Services { get; set; } = new();
    /// <summary>
    /// Gets or sets the default environment.
    /// </summary>
    /// <value>Current default environment value.</value>
    public string? DefaultEnvironment { get; set; }
    /// <summary>
    /// Gets or sets the release environment.
    /// </summary>
    /// <value>Current release environment value.</value>
    public string? ReleaseEnvironment { get; set; }
    /// <summary>
    /// Gets or sets the Jira.
    /// </summary>
    /// <value>Current Jira value.</value>
    public JiraSettings? Jira { get; set; }
    /// <summary>
    /// Gets or sets the builds.
    /// </summary>
    /// <value>Current builds value.</value>
    public BuildsSettings? Builds { get; set; }
    /// <summary>
    /// Gets or sets the Slack.
    /// </summary>
    /// <value>Current Slack value.</value>
    public SlackSettings? Slack { get; set; }
    /// <summary>
    /// Gets or sets the Microsoft Teams.
    /// </summary>
    /// <value>Current Microsoft Teams value.</value>
    public TeamsSettings? Teams { get; set; }
    /// <summary>
    /// Gets or sets the PrDeploy portal URL.
    /// </summary>
    /// <value>Current PrDeploy portal URL value.</value>
    public string? PrdeployPortalUrl { get; set; }
    /// <summary>
    /// Gets or sets the badge.
    /// </summary>
    /// <value>Current badge value.</value>
    public BadgeSettings? Badge { get; set; }
}

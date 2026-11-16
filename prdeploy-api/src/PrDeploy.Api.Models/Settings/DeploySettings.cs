namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for the deploy Settings.
/// </summary>
public class DeploySettings
{
    //// Shared by the GraphQL API and the GitHub App webhooks in PrDeploy.Api.Webhooks.
    /// <summary>
    /// Gets or sets the Deploy Workflow.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? DeployWorkflow { get; set; }
    /// <summary>
    /// Gets or sets the Environments.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public List<EnvironmentSettings>? Environments { get; set; } = new ();
    /// <summary>
    /// Gets or sets the Services.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public List<ServiceSettings>? Services { get; set; } = new();
    /// <summary>
    /// Gets or sets the Default Environment.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? DefaultEnvironment { get; set; }
    /// <summary>
    /// Gets or sets the Release Environment.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? ReleaseEnvironment { get; set; }
    /// <summary>
    /// Gets or sets the Jira.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public JiraSettings? Jira { get; set; }
    /// <summary>
    /// Gets or sets the Builds.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public BuildsSettings? Builds { get; set; }
    /// <summary>
    /// Gets or sets the Slack.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public SlackSettings? Slack { get; set; }
    /// <summary>
    /// Gets or sets the Prdeploy Portal Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? PrdeployPortalUrl { get; set; }
    /// <summary>
    /// Gets or sets the Badge.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public BadgeSettings? Badge { get; set; }
}

namespace PrDeploy.Api.Models.Settings
{
    /// <summary>
    /// Stores configuration values for the jira Settings.
    /// </summary>
    public class JiraSettings
    {
        /// <summary>
        /// Gets or sets the Add Issues Enabled.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public bool? AddIssuesEnabled { get; set; }
        /// <summary>
        /// Gets or sets the Host.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string? Host { get; set; }
        /// <summary>
        /// Gets or sets the Username.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string? Username { get; set; }
        /// <summary>
        /// Gets or sets the Password.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string? Password { get; set; }
    }
}

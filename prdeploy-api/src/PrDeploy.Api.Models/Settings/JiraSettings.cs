namespace PrDeploy.Api.Models.Settings
{
    /// <summary>Stores configuration values for Jira.</summary>
    public class JiraSettings
    {
        /// <summary>Gets or sets the add issues enabled.</summary>
        /// <value>Current add issues enabled value.</value>
        public bool? AddIssuesEnabled { get; set; }
        /// <summary>Gets or sets the host.</summary>
        /// <value>Current host value.</value>
        public string? Host { get; set; }
        /// <summary>Gets or sets the username.</summary>
        /// <value>Current username value.</value>
        public string? Username { get; set; }
        /// <summary>Gets or sets the password.</summary>
        /// <value>Current password value.</value>
        public string? Password { get; set; }
    }
}

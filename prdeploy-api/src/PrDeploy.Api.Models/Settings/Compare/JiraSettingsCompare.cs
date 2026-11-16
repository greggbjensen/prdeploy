namespace PrDeploy.Api.Models.Settings.Compare
{
    /// <summary>
    /// Represents the jira Settings Compare used by the application.
    /// </summary>
    public class JiraSettingsCompare
    {
        /// <summary>
        /// Gets or sets the Add Issues Enabled.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public OwnerRepoValue<bool?> AddIssuesEnabled { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Host.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public OwnerRepoValue<string> Host { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Username.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public OwnerRepoValue<string> Username { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Password.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public OwnerRepoValue<string> Password { get; set; } = null!;
    }
}

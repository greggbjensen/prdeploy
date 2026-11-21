namespace PrDeploy.Api.Models.Settings.Compare
{
    /// <summary>Represents Jira settings compare data used by the application.</summary>
    public class JiraSettingsCompare
    {
        /// <summary>Gets or sets the add issues enabled.</summary>
        /// <value>Current add issues enabled value.</value>
        public OwnerRepoValue<bool?> AddIssuesEnabled { get; set; } = null!;
        /// <summary>Gets or sets the host.</summary>
        /// <value>Current host value.</value>
        public OwnerRepoValue<string> Host { get; set; } = null!;
        /// <summary>Gets or sets the username.</summary>
        /// <value>Current username value.</value>
        public OwnerRepoValue<string> Username { get; set; } = null!;
        /// <summary>Gets or sets the password.</summary>
        /// <value>Current password value.</value>
        public OwnerRepoValue<string> Password { get; set; } = null!;
    }
}

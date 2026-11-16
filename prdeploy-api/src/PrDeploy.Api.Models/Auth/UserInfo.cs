namespace PrDeploy.Api.Models.Auth
{
    /// <summary>
    /// Represents user info data used by the application.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>Current id value.</value>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        /// <value>Current avatar URL value.</value>
        public string AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the login.
        /// </summary>
        /// <value>Current login value.</value>
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>Current name value.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the admin.
        /// </summary>
        /// <value>Current admin value.</value>
        public bool Admin { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>Current type value.</value>
        public GitHubAccountType? Type { get; set; }
    }
}

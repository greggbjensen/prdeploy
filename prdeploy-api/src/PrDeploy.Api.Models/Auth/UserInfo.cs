namespace PrDeploy.Api.Models.Auth
{
    /// <summary>
    /// Represents the user Info used by the application.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the Avatar Url.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Login.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string Login { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Admin.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public bool Admin { get; set; }

        /// <summary>
        /// Gets or sets the Type.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public GitHubAccountType? Type { get; set; }
    }
}

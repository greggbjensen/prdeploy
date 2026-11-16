namespace PrDeploy.Api.Business.Options
{
    /// <summary>
    /// Defines configuration options for git hub auth.
    /// </summary>
    public class GitHubAuthOptions
    {
        /// <summary>
        /// Gets or sets the authority.
        /// </summary>
        /// <value>Current authority value.</value>
        public string Authority { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the client id.
        /// </summary>
        /// <value>Current client id value.</value>
        public string ClientId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the client secret.
        /// </summary>
        /// <value>Current client secret value.</value>
        public string ClientSecret { get; set; } = string.Empty;
    }
}

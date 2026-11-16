namespace PrDeploy.Api.Business.Options
{
    /// <summary>
    /// Defines configuration options for the git Hub Auth Options.
    /// </summary>
    public class GitHubAuthOptions
    {
        /// <summary>
        /// Gets or sets the Authority.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string Authority { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Client Id.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string ClientId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Client Secret.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string ClientSecret { get; set; } = string.Empty;
    }
}

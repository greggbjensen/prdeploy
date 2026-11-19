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
        /// <summary>
        /// Gets or sets the GitHub organizations (or personal account logins) whose members may use prdeploy.
        /// </summary>
        /// <remarks>
        /// Users must be an active member of at least one listed organization to sign in, and can only reach
        /// repositories owned by a listed organization they belong to. A listed personal account is only
        /// available to that user. Matching is case-insensitive. When the list is empty nobody can sign in.
        /// </remarks>
        /// <value>Allowed organization logins, for example <c>["my-org"]</c>.</value>
        public List<string> AllowedOrganizations { get; set; } = new();
    }
}

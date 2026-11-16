namespace PrDeploy.Api.Models.Auth
{
    //// SourceRef: https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps
    /// <summary>
    /// Represents the access Token Request used by the application.
    /// </summary>
    public class AccessTokenRequest
    {
        /// <summary>
        /// Gets or sets the Grant Type.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string GrantType { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Code.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string Code { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Redirect Url.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string RedirectUrl { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Code Verifier.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string CodeVerifier { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Client Id.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string ClientId { get; set; } = string.Empty;
    }
}

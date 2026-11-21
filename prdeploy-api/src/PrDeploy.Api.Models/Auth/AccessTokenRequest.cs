namespace PrDeploy.Api.Models.Auth
{
    //// SourceRef: https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps
    /// <summary>Represents access token request data used by the application.</summary>
    public class AccessTokenRequest
    {
        /// <summary>Gets or sets the grant type.</summary>
        /// <value>Current grant type value.</value>
        public string GrantType { get; set; } = string.Empty;
        /// <summary>Gets or sets the code.</summary>
        /// <value>Current code value.</value>
        public string Code { get; set; } = string.Empty;
        /// <summary>Gets or sets the redirect URL.</summary>
        /// <value>Current redirect URL value.</value>
        public string RedirectUrl { get; set; } = string.Empty;
        /// <summary>Gets or sets the code verifier.</summary>
        /// <value>Current code verifier value.</value>
        public string CodeVerifier { get; set; } = string.Empty;
        /// <summary>Gets or sets the client id.</summary>
        /// <value>Current client id value.</value>
        public string ClientId { get; set; } = string.Empty;
    }
}

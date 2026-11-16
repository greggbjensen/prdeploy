using System.Text.Json.Serialization;

namespace PrDeploy.Api.Models.Auth
{
    /// <summary>
    /// Represents a response containing the access Token Response.
    /// </summary>
    public class AccessTokenResponse
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>Current access token value.</value>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the scope.
        /// </summary>
        /// <value>Current scope value.</value>
        [JsonPropertyName("scope")]
        public string Scope  { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token type.
        /// </summary>
        /// <value>Current token type value.</value>
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}

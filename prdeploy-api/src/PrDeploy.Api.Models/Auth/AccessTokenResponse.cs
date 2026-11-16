using System.Text.Json.Serialization;

namespace PrDeploy.Api.Models.Auth
{
    /// <summary>
    /// Represents a response containing the access Token Response.
    /// </summary>
    public class AccessTokenResponse
    {
        /// <summary>
        /// Gets or sets the Access Token.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Scope.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("scope")]
        public string Scope  { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Token Type.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}

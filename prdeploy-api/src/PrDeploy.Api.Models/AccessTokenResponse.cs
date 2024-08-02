using System.Text.Json.Serialization;

namespace PrDeploy.Api.Models
{
    public class AccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string Scope  { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}

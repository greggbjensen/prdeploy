namespace PrDeploy.Api.Models.Auth
{
    //// SourceRef: https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps
    public class AccessTokenRequest
    {
        public string GrantType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
    }
}

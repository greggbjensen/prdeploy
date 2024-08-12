namespace PrDeploy.Api.Business.Options
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string TokenEncryptionKey { get; set; } = string.Empty;
    }
}

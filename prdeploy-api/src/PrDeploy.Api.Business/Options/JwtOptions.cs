namespace PrDeploy.Api.Business.Options
{
    /// <summary>Defines configuration options for JWT.</summary>
    public class JwtOptions
    {
        /// <summary>Gets or sets the issuer.</summary>
        /// <value>Current issuer value.</value>
        public string Issuer { get; set; } = string.Empty;
        /// <summary>Gets or sets the audience.</summary>
        /// <value>Current audience value.</value>
        public string Audience { get; set; } = string.Empty;
        /// <summary>Gets or sets the key.</summary>
        /// <value>Current key value.</value>
        public string Key { get; set; } = string.Empty;
        /// <summary>Gets or sets the token encryption key.</summary>
        /// <value>Current token encryption key value.</value>
        public string TokenEncryptionKey { get; set; } = string.Empty;
    }
}

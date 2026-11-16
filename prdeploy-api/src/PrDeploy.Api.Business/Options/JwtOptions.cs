namespace PrDeploy.Api.Business.Options
{
    /// <summary>
    /// Defines configuration options for the jwt Options.
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// Gets or sets the Issuer.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string Issuer { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Audience.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string Audience { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Key.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string Key { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Token Encryption Key.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string TokenEncryptionKey { get; set; } = string.Empty;
    }
}

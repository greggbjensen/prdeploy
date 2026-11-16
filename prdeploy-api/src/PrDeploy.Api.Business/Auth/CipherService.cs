using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Business.Auth
{
    //// SourceRef: https://stackoverflow.com/questions/38795103/encrypt-string-in-net-core
    /// <summary>
    /// Coordinates cipher operations and their collaborators.
    /// </summary>
    public class CipherService : ICipherService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly JwtOptions _jwtOptions;

        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        /// <param name="dataProtectionProvider">Provider used to create data protectors for protecting and unprotecting tokens.</param>
        /// <param name="jwtOptions">JWT issuer, audience, signing-key, and token-protection configuration.</param>
        public CipherService(IDataProtectionProvider dataProtectionProvider, IOptions<JwtOptions> jwtOptions)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _jwtOptions = jwtOptions.Value;
        }

        /// <summary>
        /// Encrypts the supplied data before it is stored or transmitted.
        /// </summary>
        /// <param name="input">Plaintext text to encrypt with the configured data protector.</param>
        /// <returns>The protected representation of the plaintext input.</returns>
        public string Encrypt(string input)
        {
            var protector = _dataProtectionProvider.CreateProtector(_jwtOptions.TokenEncryptionKey);
            return protector.Protect(input);
        }

        /// <summary>
        /// Decrypts the supplied data for application use.
        /// </summary>
        /// <param name="cipherText">Encrypted text to decrypt with the configured data protector.</param>
        /// <returns>The original plaintext represented by the ciphertext.</returns>
        public string Decrypt(string cipherText)
        {
            var protector = _dataProtectionProvider.CreateProtector(_jwtOptions.TokenEncryptionKey);
            return protector.Unprotect(cipherText);
        }
    }
}

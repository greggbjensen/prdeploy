using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Business.Auth
{
    //// SourceRef: https://stackoverflow.com/questions/38795103/encrypt-string-in-net-core
    /// <summary>
    /// Coordinates operations for the cipher Service and its collaborators.
    /// </summary>
    public class CipherService : ICipherService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly JwtOptions _jwtOptions;

        /// <summary>
        /// Initializes a cipher Service with the supplied dependencies.
        /// </summary>
        /// <param name="dataProtectionProvider">Value supplied for data Protection Provider.</param>
        /// <param name="jwtOptions">Value supplied for jwt Options.</param>
        public CipherService(IDataProtectionProvider dataProtectionProvider, IOptions<JwtOptions> jwtOptions)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _jwtOptions = jwtOptions.Value;
        }

        /// <summary>
        /// Encrypts the supplied data before it is stored or transmitted.
        /// </summary>
        /// <param name="input">Input object containing the operation's arguments.</param>
        /// <returns>The value produced by this operation.</returns>
        public string Encrypt(string input)
        {
            var protector = _dataProtectionProvider.CreateProtector(_jwtOptions.TokenEncryptionKey);
            return protector.Protect(input);
        }

        /// <summary>
        /// Decrypts the supplied data for application use.
        /// </summary>
        /// <param name="cipherText">Value supplied for cipher Text.</param>
        /// <returns>The value produced by this operation.</returns>
        public string Decrypt(string cipherText)
        {
            var protector = _dataProtectionProvider.CreateProtector(_jwtOptions.TokenEncryptionKey);
            return protector.Unprotect(cipherText);
        }
    }
}

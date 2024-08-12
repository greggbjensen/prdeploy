using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Business.Auth
{
    //// SourceRef: https://stackoverflow.com/questions/38795103/encrypt-string-in-net-core
    public class CipherService : ICipherService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly JwtOptions _jwtOptions;

        public CipherService(IDataProtectionProvider dataProtectionProvider, IOptions<JwtOptions> jwtOptions)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _jwtOptions = jwtOptions.Value;
        }

        public string Encrypt(string input)
        {
            var protector = _dataProtectionProvider.CreateProtector(_jwtOptions.TokenEncryptionKey);
            return protector.Protect(input);
        }

        public string Decrypt(string cipherText)
        {
            var protector = _dataProtectionProvider.CreateProtector(_jwtOptions.TokenEncryptionKey);
            return protector.Unprotect(cipherText);
        }
    }
}

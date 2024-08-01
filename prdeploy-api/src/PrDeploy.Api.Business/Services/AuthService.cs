using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;

namespace PrDeploy.Api.Business.Services
{
    public class AuthService : IAuthService
    {
        public Task<AccessTokenResponse> GetCodeAsync(AccessTokenRequest accessTokenRequest)
        {
            return Task.FromResult(new AccessTokenResponse
            {
                AccessToken = "fun",
                Scope = "good",
                TokenType = "bearer"
            });
        }
    }
}

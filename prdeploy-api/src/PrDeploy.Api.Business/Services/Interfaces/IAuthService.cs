using PrDeploy.Api.Models;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IAuthService
{
    Task<AccessTokenResponse> GetCodeAsync(AccessTokenRequest accessTokenRequest);
}
using PrDeploy.Api.Models;

namespace PrDeploy.Api.Business.Clients.Interfaces;

public interface IGitHubAuthClient
{
    Task<AccessTokenResponse> GetAccessTokenAsync(AccessTokenRequest accessTokenRequest);
}
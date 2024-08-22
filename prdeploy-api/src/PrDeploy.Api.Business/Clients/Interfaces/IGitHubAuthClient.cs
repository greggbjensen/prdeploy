using PrDeploy.Api.Models;
using PrDeploy.Api.Models.Auth;

namespace PrDeploy.Api.Business.Clients.Interfaces;

public interface IGitHubAuthClient
{
    Task<AccessTokenResponse> GetAccessTokenAsync(AccessTokenRequest accessTokenRequest);
    Task<UserInfo> GetUserInfoAsync(string? accessToken = null);
}
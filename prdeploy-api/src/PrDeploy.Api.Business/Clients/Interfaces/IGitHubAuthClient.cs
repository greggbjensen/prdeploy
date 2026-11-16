using PrDeploy.Api.Models;
using PrDeploy.Api.Models.Auth;

namespace PrDeploy.Api.Business.Clients.Interfaces;

/// <summary>
/// Provides client operations for communicating with the i Git Hub Auth Client.
/// </summary>
public interface IGitHubAuthClient
{
    /// <summary>
    /// Retrieves the Access Token.
    /// </summary>
    /// <param name="accessTokenRequest">Value supplied for access Token Request.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<AccessTokenResponse> GetAccessTokenAsync(AccessTokenRequest accessTokenRequest);
    /// <summary>
    /// Retrieves the User Info.
    /// </summary>
    /// <param name="accessToken">Value supplied for access Token.</param>
    /// <returns>The value produced by this operation.</returns>
    Task<UserInfo> GetUserInfoAsync(string? accessToken = null);
}
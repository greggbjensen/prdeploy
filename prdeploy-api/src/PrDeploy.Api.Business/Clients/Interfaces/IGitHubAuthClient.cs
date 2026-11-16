using PrDeploy.Api.Models;
using PrDeploy.Api.Models.Auth;

namespace PrDeploy.Api.Business.Clients.Interfaces;

/// <summary>
/// Provides client operations for communicating with the i Git Hub Auth Client.
/// </summary>
public interface IGitHubAuthClient
{
    /// <summary>
    /// Retrieves the access token.
    /// </summary>
    /// <param name="accessTokenRequest">Object containing the access token request data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    Task<AccessTokenResponse> GetAccessTokenAsync(AccessTokenRequest accessTokenRequest);
    /// <summary>
    /// Retrieves the user info.
    /// </summary>
    /// <param name="accessToken">Text used as the access token.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    Task<UserInfo> GetUserInfoAsync(string? accessToken = null);
}
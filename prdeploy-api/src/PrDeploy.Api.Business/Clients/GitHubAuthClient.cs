using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Octokit;
using Octokit.Internal;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Clients.Interfaces;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Models.Auth;
using RestSharp;

namespace PrDeploy.Api.Business.Clients
{
    /// <summary>
    /// Provides client operations for communicating with the git Hub Auth Client.
    /// </summary>
    public class GitHubAuthClient : IGitHubAuthClient
    {
        private readonly ICipherService _cipherService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IValidator<AccessTokenRequest> _validator;
        private readonly IRestClient _client;
        private readonly GitHubAuthOptions _gitHubOptions;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<GitHubAuthClient> _logger;

        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        /// <param name="restClientInstance">Object containing the rest client instance data used by the operation.</param>
        /// <param name="jwtOptions">JWT issuer, audience, signing-key, and token-protection configuration.</param>
        /// <param name="cipherService">Object containing the cipher service data used by the operation.</param>
        /// <param name="serviceProvider">Object containing the service provider data used by the operation.</param>
        /// <param name="validator">Object containing the validator data used by the operation.</param>
        /// <param name="logger">Logger used to report authentication activity without logging tokens or secrets.</param>
        public GitHubAuthClient(IRestClientInstance<GitHubAuthOptions> restClientInstance,
            IOptions<JwtOptions> jwtOptions,
            ICipherService cipherService,
            IServiceProvider serviceProvider,
            IValidator<AccessTokenRequest> validator,
            ILogger<GitHubAuthClient> logger)
        {
            _jwtOptions = jwtOptions.Value;
            _cipherService = cipherService;
            _serviceProvider = serviceProvider;
            _validator = validator;
            _logger = logger;
            _client = restClientInstance.RestClient;
            _gitHubOptions = restClientInstance.Options;
        }

        /// <summary>
        /// Retrieves the access token.
        /// </summary>
        /// <param name="accessTokenRequest">Object containing the access token request data used by the operation.</param>
        /// <returns>Requested value, or its default when no value is available.</returns>
        public async Task<AccessTokenResponse> GetAccessTokenAsync(AccessTokenRequest accessTokenRequest)
        {
            await _validator.ValidateAndThrowAsync(accessTokenRequest);

            if (accessTokenRequest.ClientId != _gitHubOptions.ClientId)
            {
                _logger.LogWarning("Rejected access token request with a client ID that does not match the configured client.");
                throw new HttpRequestException("Invalid Client ID.", null, HttpStatusCode.Forbidden);
            }

            var formValues = new Dictionary<string, string?>
            {
                ["client_id"] = _gitHubOptions.ClientId,
                ["client_secret"] = _gitHubOptions.ClientSecret,
                ["code"] = accessTokenRequest.Code,
                ["redirect_uri"] = accessTokenRequest.RedirectUrl
            };
            
            // Add code_verifier if provided (required for PKCE flow)
            if (!string.IsNullOrEmpty(accessTokenRequest.CodeVerifier))
            {
                formValues["code_verifier"] = accessTokenRequest.CodeVerifier;
            }

            using var formContent = new FormUrlEncodedContent(formValues);
            var formBody = await formContent.ReadAsStringAsync();
            var request = new RestRequest("login/oauth/access_token", Method.Post);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("application/x-www-form-urlencoded", formBody, ParameterType.RequestBody);

            var responseValue = await _client.PostAsync(request);
            AccessTokenResponse? accessTokenResponse = null;
            if (!string.IsNullOrWhiteSpace(responseValue?.Content))
            {
                try
                {
                    accessTokenResponse = JsonSerializer.Deserialize<AccessTokenResponse>(responseValue.Content);
                }
                catch (JsonException)
                {
                    accessTokenResponse = null;
                }
            }

            if (string.IsNullOrWhiteSpace(accessTokenResponse?.AccessToken))
            {
                var (oauthError, oauthErrorDescription) = ExtractOAuthError(responseValue?.Content);
                _logger.LogWarning(
                    "GitHub did not return an access token (HTTP status {StatusCode}, error {OAuthError}: {OAuthErrorDescription}).",
                    (int)(responseValue?.StatusCode ?? 0),
                    oauthError ?? "unknown",
                    oauthErrorDescription ?? "none");
                throw new HttpRequestException("Error getting access token", null, HttpStatusCode.InternalServerError);
            }

            var userInfo = await GetUserInfoAsync(accessTokenResponse.AccessToken);

            // Swap GitHub access token for full signed JWT.
            accessTokenResponse.AccessToken = CreateJwt(accessTokenResponse.AccessToken, userInfo);
            _logger.LogInformation("Issued an access token for {Login}.", userInfo.Login);

            return accessTokenResponse;
        }

        /// <summary>
        /// Extracts non-sensitive OAuth error fields from a GitHub token endpoint response body.
        /// </summary>
        /// <param name="content">Raw JSON payload returned by the token endpoint.</param>
        /// <returns>The OAuth <c>error</c> code and <c>error_description</c> message when present.</returns>
        private static (string? Error, string? ErrorDescription) ExtractOAuthError(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return (null, null);
            }

            try
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return (null, null);
                }

                var error = document.RootElement.TryGetProperty("error", out var errorProp) &&
                            errorProp.ValueKind == JsonValueKind.String
                    ? errorProp.GetString()
                    : null;
                var errorDescription = document.RootElement.TryGetProperty("error_description", out var descProp) &&
                                       descProp.ValueKind == JsonValueKind.String
                    ? descProp.GetString()
                    : null;

                return (error, errorDescription);
            }
            catch (JsonException)
            {
                return (null, null);
            }
        }

        /// <summary>
        /// Retrieves the user info.
        /// </summary>
        /// <param name="accessToken">Text used as the access token.</param>
        /// <returns>Requested value, or its default when no value is available.</returns>
        public async Task<UserInfo> GetUserInfoAsync(string? accessToken = null)
        {
            // We have to resolve this here to make sure there is a token.
            IGitHubClient gitHubClient;
            if (string.IsNullOrEmpty(accessToken))
            {
                gitHubClient = _serviceProvider.GetRequiredService<IGitHubClient>();
            }
            else if (_serviceProvider.GetService<ISecurityContext>() == null &&
                     _serviceProvider.GetService<IGitHubClient>() is { } registeredClient)
            {
                gitHubClient = registeredClient;
            }
            else
            {
                var credentials = new Credentials(accessToken);
                gitHubClient = new GitHubClient(
                    new ProductHeaderValue("prdeploy"), new InMemoryCredentialStore(credentials));
            }

            try
            {
                var user = await gitHubClient.User.Current();
                _logger.LogDebug("Retrieved GitHub user info for {Login}.", user.Login);
                var userInfo = Map.UserInfo(user);
                return userInfo ?? new UserInfo();
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "GitHub API error retrieving user info (HTTP status {StatusCode}).", (int)ex.StatusCode);
                throw new HttpRequestException("Error getting user info from GitHub.", ex, ex.StatusCode);
            }
        }

        /// <summary>
        /// Creates the JWT from the supplied inputs.
        /// </summary>
        /// <param name="gitHubToken">Text used as the git hub token.</param>
        /// <param name="userInfo">Object containing the user info data used by the operation.</param>
        /// <returns>The object created from the supplied inputs.</returns>
        private string CreateJwt(string gitHubToken, UserInfo userInfo)
        {
            var encryptedToken = _cipherService.Encrypt(gitHubToken);
            var key = Encoding.UTF8.GetBytes(_jwtOptions.Key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, encryptedToken),
                    new Claim(JwtRegisteredClaimNames.Name, userInfo.Login),
                    new Claim(JwtRegisteredClaimNames.Jti,
                        Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(8), // Same time as GitHub token expiration.
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha512Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var stringToken = tokenHandler.WriteToken(token);

            return stringToken;
        }
    }
}

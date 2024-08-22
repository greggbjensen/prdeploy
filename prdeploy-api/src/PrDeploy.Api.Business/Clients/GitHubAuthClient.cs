using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
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
    public class GitHubAuthClient : IGitHubAuthClient
    {
        private readonly ICipherService _cipherService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IValidator<AccessTokenRequest> _validator;
        private readonly IRestClient _client;
        private readonly GitHubAuthOptions _gitHubOptions;
        private readonly JwtOptions _jwtOptions;    

        public GitHubAuthClient(IRestClientInstance<GitHubAuthOptions> restClientInstance,
            IOptions<JwtOptions> jwtOptions,
            ICipherService cipherService,
            IServiceProvider serviceProvider,
            IValidator<AccessTokenRequest> validator)
        {
            _jwtOptions = jwtOptions.Value;
            _cipherService = cipherService;
            _serviceProvider = serviceProvider;
            _validator = validator;
            _client = restClientInstance.RestClient;
            _gitHubOptions = restClientInstance.Options;
        }

        public async Task<AccessTokenResponse> GetAccessTokenAsync(AccessTokenRequest accessTokenRequest)
        {
            await _validator.ValidateAndThrowAsync(accessTokenRequest);

            if (accessTokenRequest.ClientId != _gitHubOptions.ClientId)
            {
                throw new HttpRequestException("Invalid Client ID.", null, HttpStatusCode.Forbidden);
            }

            var request = new RestRequest("login/oauth/access_token");
            request.AddQueryParameter("client_id", _gitHubOptions.ClientId);
            request.AddQueryParameter("client_secret", _gitHubOptions.ClientSecret);
            request.AddQueryParameter("code", accessTokenRequest.Code);
            request.AddQueryParameter("redirect_uri", accessTokenRequest.RedirectUrl);

            var response = await _client.PostAsync<AccessTokenResponse>(request);
            var userInfo = await GetUserInfoAsync(response!.AccessToken);

            // Swap GitHub access token for full signed JWT.
            response!.AccessToken = CreateJwt(response!.AccessToken, userInfo);

            return response!;
        }

        public async Task<UserInfo> GetUserInfoAsync(string? accessToken = null)
        {
            // We have to resolve this here to make sure there is a token.
            IGitHubClient gitHubClient;
            if (string.IsNullOrEmpty(accessToken))
            {
                gitHubClient = _serviceProvider.GetRequiredService<IGitHubClient>();
            }
            else
            {
                var credentials = new Credentials(accessToken);
                gitHubClient = new GitHubClient(
                    new ProductHeaderValue("prdeploy"), new InMemoryCredentialStore(credentials));
            }

            var user = await gitHubClient.User.Current();
            var userInfo = Map.UserInfo(user);
            return userInfo ?? new UserInfo();
        }

        private string CreateJwt(string gitHubToken, UserInfo userInfo)
        {
            var encryptedToken = _cipherService.Encrypt(gitHubToken);
            var key = Encoding.ASCII.GetBytes(_jwtOptions.Key);
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

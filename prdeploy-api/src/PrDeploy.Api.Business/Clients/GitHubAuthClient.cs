using System.Net;
using FluentValidation;
using Octokit;
using PrDeploy.Api.Business.Clients.Interfaces;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Models;
using RestSharp;

namespace PrDeploy.Api.Business.Clients
{
    public class GitHubAuthClient : IGitHubAuthClient
    {
        private readonly IValidator<AccessTokenRequest> _validator;
        private readonly IRestClient _client;
        private readonly GitHubAuthOptions _options;

        public GitHubAuthClient(IRestClientInstance<GitHubAuthOptions> restClientInstance, IValidator<AccessTokenRequest> validator)
        {
            _validator = validator;
            _client = restClientInstance.RestClient;
            _options = restClientInstance.Options;
        }

        public async Task<AccessTokenResponse> GetAccessTokenAsync(AccessTokenRequest accessTokenRequest)
        {
            await _validator.ValidateAndThrowAsync(accessTokenRequest);

            if (accessTokenRequest.ClientId != _options.ClientId)
            {
                throw new HttpRequestException("Invalid Client ID.", null, HttpStatusCode.Forbidden);
            }

            var request = new RestRequest("login/oauth/access_token");
            request.AddQueryParameter("client_id", _options.ClientId);
            request.AddQueryParameter("client_secret", _options.ClientSecret);
            request.AddQueryParameter("code", accessTokenRequest.Code);
            request.AddQueryParameter("redirect_uri", accessTokenRequest.RedirectUrl);

            var response = await _client.PostAsync<AccessTokenResponse>(request);
            return response!;
        }
    }
}

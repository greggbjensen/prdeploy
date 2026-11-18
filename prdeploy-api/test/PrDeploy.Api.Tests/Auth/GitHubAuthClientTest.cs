using System.Net;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Clients;
using PrDeploy.Api.Business.Clients.Interfaces;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Models.Auth;
using PrDeploy.Api.Tests.Webhooks.Framework;
using RestSharp;

namespace PrDeploy.Api.Tests.Auth;

/// <summary>
/// Contains tests that verify git hub authentication client behavior.
/// </summary>
public class GitHubAuthClientTest
{
    /// <summary>
    /// Verifies that RestSharp maps GitHub's JSON token field names.
    /// </summary>
    [Fact]
    public async Task PostAsync_MapsGitHubTokenResponseFields()
    {
        var handler = new FakeHttpMessageHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"access_token":"github-token","scope":"repo user","token_type":"bearer"}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            }
        };
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://github.com/")
        };
        var restClient = new RestClient(httpClient);

        var response = await restClient.PostAsync<AccessTokenResponse>(
            new RestRequest("login/oauth/access_token", Method.Post));

        Assert.NotNull(response);
        Assert.Equal("github-token", response.AccessToken);
        Assert.Equal("repo user", response.Scope);
        Assert.Equal("bearer", response.TokenType);
    }

    /// <summary>
    /// Verifies that access token exchange parameters are posted in an encoded form body.
    /// </summary>
    [Fact]
    public async Task GetAccessTokenAsync_PostsOAuthParametersInFormBody()
    {
        var handler = new FakeHttpMessageHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"access_token":"","scope":"repo user","token_type":"bearer"}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            }
        };
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://github.com/")
        };
        var restClient = new RestClient(httpClient);
        var options = new GitHubAuthOptions
        {
            Authority = "https://github.com/",
            ClientId = "client id+1",
            ClientSecret = "secret&one=two"
        };
        var restClientInstance = new RestClientInstance<GitHubAuthOptions>
        {
            Options = options,
            RestClient = restClient
        };

        var gitHub = new Mock<IGitHubClient>(MockBehavior.Strict);
        gitHub.Setup(x => x.User.Current()).ReturnsAsync(
            GitHubModels.FromJson<User>(
                """{"login":"octocat","name":"Octo Cat","email":"","id":1}"""));

        var services = new ServiceCollection();
        services.AddSingleton(gitHub.Object);
        using var serviceProvider = services.BuildServiceProvider();

        var cipher = new Mock<ICipherService>(MockBehavior.Strict);
        cipher.Setup(x => x.Encrypt(string.Empty)).Returns("encrypted-token");

        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "test",
            Audience = "test",
            Key = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
        });
        var validator = new InlineValidator<AccessTokenRequest>();
        var client = new GitHubAuthClient(
            restClientInstance,
            jwtOptions,
            cipher.Object,
            serviceProvider,
            validator);

        var response = await client.GetAccessTokenAsync(new AccessTokenRequest
        {
            ClientId = options.ClientId,
            Code = "code+with spaces&symbols",
            RedirectUrl = "https://example.com/callback?foo=a&b=c",
            CodeVerifier = "verifier+with spaces"
        });

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recorded.Request.Method);
        Assert.Contains(
            recorded.Request.Headers.Accept,
            header => header.MediaType == "application/json");
        Assert.Equal(string.Empty, recorded.Request.RequestUri!.Query);
        Assert.Equal(
            "application/x-www-form-urlencoded",
            recorded.Request.Content!.Headers.ContentType!.MediaType);
        Assert.Equal(
            "client_id=client+id%2B1&client_secret=secret%26one%3Dtwo&code=code%2Bwith+spaces%26symbols&redirect_uri=https%3A%2F%2Fexample.com%2Fcallback%3Ffoo%3Da%26b%3Dc&code_verifier=verifier%2Bwith+spaces",
            recorded.Body);
        Assert.Equal("repo user", response.Scope);
        Assert.Equal("bearer", response.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
    }
}

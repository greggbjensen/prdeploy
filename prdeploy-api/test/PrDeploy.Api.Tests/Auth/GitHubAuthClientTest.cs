using System.Text.Json;
using System.Net;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Clients;
using PrDeploy.Api.Business.Clients.Interfaces;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Models.Auth;
using PrDeploy.Api.Tests.Webhooks.Framework;
using RestSharp;

namespace PrDeploy.Api.Tests.Auth;

/// <summary>Contains tests that verify git hub authentication client behavior.</summary>
public class GitHubAuthClientTest
{
    /// <summary>Creates an organization access double that treats every user as a member of an allowed organization.</summary>
    /// <returns>Organization access that always allows sign in.</returns>
    private static IOrganizationAccess AllowAllOrganizations()
    {
        var access = new Mock<IOrganizationAccess>();
        access.Setup(a => a.IsMemberOfAnyAllowedAsync(It.IsAny<IGitHubClient>(), It.IsAny<string>())).ReturnsAsync(true);
        return access.Object;
    }

    /// <summary>Verifies that RestSharp maps GitHub's JSON token field names.</summary>
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

    /// <summary>Verifies that access token exchange parameters are posted in an encoded form body and exchanged for a signed JWT.</summary>
    [Fact]
    public async Task GetAccessTokenAsync_PostsOAuthParametersInFormBody()
    {
        var handler = new FakeHttpMessageHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"access_token":"ghu_test","scope":"repo user","token_type":"bearer"}""",
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
        cipher.Setup(x => x.Encrypt("ghu_test")).Returns("encrypted-token");

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
            validator,
            AllowAllOrganizations(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GitHubAuthClient>.Instance);

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
        Assert.NotEqual("ghu_test", response.AccessToken);
        cipher.Verify(x => x.Encrypt("ghu_test"), Times.Once);
    }

    /// <summary>Verifies that an OAuth error response logs sanitized error details and throws an <see cref="HttpRequestException"/>.</summary>
    [Fact]
    public async Task GetAccessTokenAsync_WhenGitHubReturnsOAuthError_LogsWarningAndThrows()
    {
        var handler = new FakeHttpMessageHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"error":"bad_verification_code","error_description":"The code passed is incorrect or expired.","secret_field":"do-not-log"}""",
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
            ClientId = "client-id",
            ClientSecret = "client-secret"
        };
        var restClientInstance = new RestClientInstance<GitHubAuthOptions>
        {
            Options = options,
            RestClient = restClient
        };

        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var cipher = new Mock<ICipherService>(MockBehavior.Strict);
        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "test",
            Audience = "test",
            Key = "0123456789abcdef0123456789abcdef0123456789abcdef"
        });
        var validator = new InlineValidator<AccessTokenRequest>();
        var logger = new PrDeploy.Api.Tests.Framework.CapturingLogger<GitHubAuthClient>();
        var client = new GitHubAuthClient(
            restClientInstance,
            jwtOptions,
            cipher.Object,
            serviceProvider,
            validator,
            AllowAllOrganizations(),
            logger);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAccessTokenAsync(new AccessTokenRequest
            {
                ClientId = options.ClientId,
                Code = "expired-code",
                RedirectUrl = "https://example.com/callback"
            }));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("bad_verification_code", entry.Properties["OAuthError"]);
        Assert.Equal("The code passed is incorrect or expired.", entry.Properties["OAuthErrorDescription"]);
        Assert.DoesNotContain("do-not-log", entry.Message, StringComparison.Ordinal);
    }

    /// <summary>Verifies that a token response that is not valid JSON is logged as a structured warning that carries the parse exception and HTTP status but never the response body.</summary>
    [Fact]
    public async Task GetAccessTokenAsync_InvalidJsonResponse_LogsParseWarningWithoutBody()
    {
        var handler = new FakeHttpMessageHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{not-json secret-body", System.Text.Encoding.UTF8, "application/json")
            }
        };
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://github.com/") };
        var options = new GitHubAuthOptions
        {
            Authority = "https://github.com/",
            ClientId = "client-id",
            ClientSecret = "client-secret"
        };
        var restClientInstance = new RestClientInstance<GitHubAuthOptions>
        {
            Options = options,
            RestClient = new RestClient(httpClient)
        };
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = new PrDeploy.Api.Tests.Framework.CapturingLogger<GitHubAuthClient>();
        var client = new GitHubAuthClient(
            restClientInstance,
            Microsoft.Extensions.Options.Options.Create(new JwtOptions()),
            new Mock<ICipherService>(MockBehavior.Strict).Object,
            serviceProvider,
            new InlineValidator<AccessTokenRequest>(),
            AllowAllOrganizations(),
            logger);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAccessTokenAsync(new AccessTokenRequest
        {
            ClientId = options.ClientId,
            Code = "code",
            RedirectUrl = "https://example.com/callback"
        }));

        var parseEntry = Assert.Single(logger.Entries, e => e.Exception is JsonException);
        Assert.Equal(LogLevel.Warning, parseEntry.Level);
        Assert.Equal(200, parseEntry.Properties["StatusCode"]);
        Assert.All(logger.Entries, e => Assert.DoesNotContain("secret-body", e.Message, StringComparison.Ordinal));
    }

    /// <summary>Verifies that an Octokit authorization failure during user info lookup is translated into an <see cref="HttpRequestException"/> with the upstream status code.</summary>
    [Fact]
    public async Task GetUserInfoAsync_WhenGitHubThrowsAuthorizationException_ThrowsHttpRequestExceptionWithUnauthorizedStatus()
    {
        var gitHub = new Mock<IGitHubClient>(MockBehavior.Strict);
        gitHub.Setup(x => x.User.Current()).ThrowsAsync(new AuthorizationException());

        var services = new ServiceCollection();
        services.AddSingleton(gitHub.Object);
        using var serviceProvider = services.BuildServiceProvider();

        var restClientInstance = new RestClientInstance<GitHubAuthOptions>
        {
            Options = new GitHubAuthOptions(),
            RestClient = new RestClient()
        };
        var logger = new PrDeploy.Api.Tests.Framework.CapturingLogger<GitHubAuthClient>();
        var client = new GitHubAuthClient(
            restClientInstance,
            Microsoft.Extensions.Options.Options.Create(new JwtOptions()),
            new Mock<ICipherService>(MockBehavior.Strict).Object,
            serviceProvider,
            new InlineValidator<AccessTokenRequest>(),
            AllowAllOrganizations(),
            logger);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetUserInfoAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(401, entry.Properties["StatusCode"]);
    }

    /// <summary>Verifies that a user who is not a member of any allowed organization is rejected with Forbidden and no JWT is issued.</summary>
    [Fact]
    public async Task GetAccessTokenAsync_NotMemberOfAllowedOrganization_ThrowsForbidden()
    {
        var handler = new FakeHttpMessageHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"access_token":"ghu_test","scope":"repo read:org","token_type":"bearer"}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            }
        };
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://github.com/") };
        var options = new GitHubAuthOptions { Authority = "https://github.com/", ClientId = "id", ClientSecret = "s" };

        var gitHub = new Mock<IGitHubClient>();
        gitHub.Setup(x => x.User.Current()).ReturnsAsync(
            GitHubModels.FromJson<User>("""{"login":"outsider","name":"Out Sider","email":"","id":2}"""));
        var services = new ServiceCollection();
        services.AddSingleton(gitHub.Object);
        using var serviceProvider = services.BuildServiceProvider();

        var organizationAccess = new Mock<IOrganizationAccess>();
        organizationAccess.Setup(a => a.IsMemberOfAnyAllowedAsync(gitHub.Object, "outsider")).ReturnsAsync(false);
        var cipher = new Mock<ICipherService>(MockBehavior.Strict);
        var client = new GitHubAuthClient(
            new RestClientInstance<GitHubAuthOptions> { Options = options, RestClient = new RestClient(httpClient) },
            Microsoft.Extensions.Options.Options.Create(new JwtOptions { Key = new string('k', 64) }),
            cipher.Object,
            serviceProvider,
            new InlineValidator<AccessTokenRequest>(),
            organizationAccess.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GitHubAuthClient>.Instance);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAccessTokenAsync(new AccessTokenRequest { ClientId = "id", Code = "code" }));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        cipher.Verify(x => x.Encrypt(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifies that a JWT issued by <see cref="GitHubAuthClient.GetAccessTokenAsync"/> is accepted by <see cref="PrDeploy.Api.Auth.GitHubAuthenticationHandler"/> and restores the decrypted GitHub token into <see cref="ISecurityContext"/>.</summary>
    [Fact]
    public async Task GetAccessTokenAsync_IssuedJwt_AuthenticatesAndDecryptsGitHubTokenInHandler()
    {
        var handler = new FakeHttpMessageHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"access_token":"ghu_live_token_value","scope":"repo read:org","token_type":"bearer"}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            }
        };
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://github.com/")
        };
        var restClient = new RestClient(httpClient);
        var gitHubOptions = new GitHubAuthOptions
        {
            Authority = "https://github.com",
            ClientId = "Ov23testClientId",
            ClientSecret = "test-client-secret"
        };
        var jwtConfig = new JwtOptions
        {
            Issuer = "http://localhost:4302",
            Audience = "http://localhost:4301",
            Key = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            TokenEncryptionKey = "encryption-purpose-key"
        };

        var gitHub = new Mock<IGitHubClient>(MockBehavior.Strict);
        gitHub.Setup(x => x.User.Current()).ReturnsAsync(
            GitHubModels.FromJson<User>(
                """{"login":"octocat","name":"Octo Cat","email":"","id":1}"""));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddScoped<ISecurityContext, PrDeploy.Api.Auth.SecurityContext>();
        services.Configure<JwtOptions>(o =>
        {
            o.Issuer = jwtConfig.Issuer;
            o.Audience = jwtConfig.Audience;
            o.Key = jwtConfig.Key;
            o.TokenEncryptionKey = jwtConfig.TokenEncryptionKey;
        });
        services.AddScoped<ICipherService, PrDeploy.Api.Business.Auth.CipherService>();
        PrDeploy.Api.Auth.AuthenticationExtensions.AddGitHubAuthentication(
            services,
            o =>
            {
                o.Authority = gitHubOptions.Authority;
                o.ClientId = gitHubOptions.ClientId;
                o.ClientSecret = gitHubOptions.ClientSecret;
            },
            o =>
            {
                o.Issuer = jwtConfig.Issuer;
                o.Audience = jwtConfig.Audience;
                o.Key = jwtConfig.Key;
                o.TokenEncryptionKey = jwtConfig.TokenEncryptionKey;
            });

        await using var serviceProvider = services.BuildServiceProvider();

        // Use a separate minimal provider without ISecurityContext so GetAccessTokenAsync uses the mocked IGitHubClient.
        var tokenExchangeServices = new ServiceCollection();
        tokenExchangeServices.AddSingleton(gitHub.Object);
        await using var tokenExchangeProvider = tokenExchangeServices.BuildServiceProvider();

        var cipherService = serviceProvider.GetRequiredService<ICipherService>();
        var authClient = new GitHubAuthClient(
            new RestClientInstance<GitHubAuthOptions> { Options = gitHubOptions, RestClient = restClient },
            Microsoft.Extensions.Options.Options.Create(jwtConfig),
            cipherService,
            tokenExchangeProvider,
            new PrDeploy.Api.Models.Auth.Validators.AccessTokenRequestValidator(),
            AllowAllOrganizations(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GitHubAuthClient>.Instance);

        var tokenResponse = await authClient.GetAccessTokenAsync(new AccessTokenRequest
        {
            ClientId = gitHubOptions.ClientId,
            Code = "valid-oauth-code",
            RedirectUrl = "http://localhost:4301/login/callback",
            CodeVerifier = "pkce-code-verifier"
        });

        // Now authenticate an incoming HTTP request bearing the issued JWT.
        using var scope = serviceProvider.CreateScope();
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        httpContext.Request.Headers.Authorization = $"Bearer {tokenResponse.AccessToken}";
        scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext = httpContext;

        var authHandlerProvider = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationHandlerProvider>();
        var authHandler = await authHandlerProvider.GetHandlerAsync(
            httpContext,
            Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
        Assert.NotNull(authHandler);

        var authResult = await authHandler.AuthenticateAsync();
        Assert.True(authResult.Succeeded, authResult.Failure?.Message);

        var securityContext = scope.ServiceProvider.GetRequiredService<ISecurityContext>();
        Assert.Equal("ghu_live_token_value", securityContext.UserToken);
        Assert.Equal("octocat", securityContext.Login);
    }
}

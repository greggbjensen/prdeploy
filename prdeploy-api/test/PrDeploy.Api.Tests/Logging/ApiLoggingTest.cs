using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using HotChocolate;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using PrDeploy.Api.Filters;
using PrDeploy.Api.Middleware;
using PrDeploy.Api.Tests.Framework;

namespace PrDeploy.Api.Tests.Logging;

/// <summary>Verifies structured logging in the API's error filter and middleware.</summary>
public class ApiLoggingTest
{
    /// <summary>Logger that records the scope state passed to <see cref="ILogger.BeginScope{TState}"/>.</summary>
    /// <typeparam name="T">Category type.</typeparam>
    private sealed class ScopeLogger<T> : ILogger<T>
    {
        public List<object> Scopes { get; } = new();

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            Scopes.Add(state);
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }

    /// <summary>Creates the filter under test together with its capturing logger.</summary>
    /// <param name="logger">Receives the logger used by the filter.</param>
    /// <returns>The filter.</returns>
    private static SanitizedErrorFilter CreateFilter(out CapturingLogger<SanitizedErrorFilter> logger)
    {
        logger = new CapturingLogger<SanitizedErrorFilter>();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Production");
        return new SanitizedErrorFilter(logger, environment.Object);
    }

    /// <summary>Confirms forbidden errors are warnings and the message is a template, not interpolated text.</summary>
    [Fact]
    public void OnError_Forbidden_LogsWarningWithStructuredProperties()
    {
        var filter = CreateFilter(out var logger);
        var error = ErrorBuilder.New().SetMessage("Access denied")
            .SetException(new HttpRequestException("x", null, HttpStatusCode.Forbidden)).Build();

        filter.OnError(error);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("FORBIDDEN", entry.Properties["ErrorCode"]);
        Assert.Contains("{ErrorMessage}", entry.Template);
    }

    /// <summary>Confirms Octokit authorization failures map to UNAUTHORIZED and log as a warning.</summary>
    [Fact]
    public void OnError_OctokitAuthorizationException_MapsToUnauthorizedWarning()
    {
        var filter = CreateFilter(out var logger);
        var error = ErrorBuilder.New().SetMessage("Bad credentials")
            .SetException(new Octokit.AuthorizationException()).Build();

        var result = filter.OnError(error);

        Assert.Equal("UNAUTHORIZED", result.Code);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("UNAUTHORIZED", entry.Properties["ErrorCode"]);
    }

    /// <summary>Confirms validation failures are warnings.</summary>
    [Fact]
    public void OnError_Validation_LogsWarning()
    {
        var filter = CreateFilter(out var logger);
        var error = ErrorBuilder.New().SetMessage("bad")
            .SetException(new ValidationException(new[] { new ValidationFailure("A", "bad") })).Build();

        filter.OnError(error);

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    /// <summary>Confirms an unexpected exception is logged at Error with the exception attached.</summary>
    [Fact]
    public void OnError_Unexpected_LogsErrorWithException()
    {
        var filter = CreateFilter(out var logger);
        var failure = new InvalidOperationException("boom");
        var error = ErrorBuilder.New().SetMessage("boom").SetException(failure).Build();

        filter.OnError(error);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(failure, entry.Exception);
    }

    /// <summary>Confirms an authenticated request runs inside a scope carrying the login.</summary>
    [Fact]
    public async Task UserLogScopeMiddleware_Authenticated_OpensUserScope()
    {
        var logger = new ScopeLogger<UserLogScopeMiddleware>();
        var called = false;
        var middleware = new UserLogScopeMiddleware(_ => { called = true; return Task.CompletedTask; }, logger);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(JwtRegisteredClaimNames.Name, "octocat") }, "test"))
        };

        await middleware.InvokeAsync(context);

        Assert.True(called);
        var scope = Assert.IsType<Dictionary<string, object>>(Assert.Single(logger.Scopes));
        Assert.Equal("octocat", scope[UserLogScopeMiddleware.UserProperty]);
    }

    /// <summary>Confirms anonymous requests pass through without a user scope.</summary>
    [Fact]
    public async Task UserLogScopeMiddleware_Anonymous_OpensNoScope()
    {
        var logger = new ScopeLogger<UserLogScopeMiddleware>();
        var called = false;
        var middleware = new UserLogScopeMiddleware(_ => { called = true; return Task.CompletedTask; }, logger);

        await middleware.InvokeAsync(new DefaultHttpContext());

        Assert.True(called);
        Assert.Empty(logger.Scopes);
    }

    /// <summary>Confirms the GraphQL standards middleware logs headers as Debug template properties.</summary>
    [Fact]
    public async Task GraphQlStandardsMiddleware_LogsHeadersAsDebugProperties()
    {
        var logger = new CapturingLogger<GraphQlStandardsMiddleware>();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Production");
        var middleware = new GraphQlStandardsMiddleware(_ => Task.CompletedTask, logger, environment.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers.Accept = "application/json";
        context.Response.ContentType = "application/json";

        await middleware.InvokeAsync(context);

        Assert.All(logger.Entries, e => Assert.Equal(LogLevel.Debug, e.Level));
        Assert.Equal("application/json", logger.Entries[0].Properties["RequestAccept"]);
        Assert.Contains(logger.Entries, e => e.Properties.ContainsKey("ResponseContentType"));
    }
}

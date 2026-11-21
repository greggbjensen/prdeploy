using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PrDeploy.Api;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Builder;
using PrDeploy.Api.Business;
using PrDeploy.Api.Business.Clients.Interfaces;
using PrDeploy.Api.Business.Stores;
using PrDeploy.Api.Configuration;
using PrDeploy.Api.Middleware;
using PrDeploy.Api.Models;
using PrDeploy.Api.Models.Auth;
using PrDeploy.Api.Options;
using PrDeploy.Api.Webhooks;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddOpenApi();

    var configuration = builder.Configuration
        // Swap the default appsettings JSON sources for their YAML equivalents.
        .UseYamlAppSettings(builder.Environment.EnvironmentName)
        // This is loaded in the Kubernetes cluster as a mounted secret.
        .AddYamlFile("secrets/appsettings.secret.yaml", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

    //initial logger without env running
    Log.Logger = SerilogConfiguration.Configure(
            new LoggerConfiguration(), configuration, prettyConsole: builder.Environment.IsDevelopment())
        .CreateBootstrapLogger();

    builder.Services
        .AddPrDeployApi(configuration)
        .AddPrDeployApiBusiness(configuration)
        .AddPrDeployApiModelValidation()
        .AddPrDeployWebhooks(configuration)
        .AddGitHubAuthentication(
            options => configuration.Bind("GitHubAuth", options),
            options => configuration.Bind("Jwt", options))
        .AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireGitHubRepoAccess()
                .Build();
        })
        .AddOptions()
        .Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        })
        .AddCors(options =>
        {
            var corsOptions = new CorsOptions();
            configuration.Bind("Cors", corsOptions);
            options.AddDefaultPolicy(
                policy =>
                {
                    policy.WithOrigins(corsOptions.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        })
        .AddHealthChecks()
        .AddCheck<HealthCheck>("Running", null, new[] { "live", "ready" });

    // Replaces the bootstrap logger so the host logger honors the final configuration and DI-registered sinks.
    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        SerilogConfiguration.Configure(
            loggerConfiguration, context.Configuration, services, context.HostingEnvironment.IsDevelopment()));
    var app = builder.Build();
    Log.Information("Starting PrDeploy API in {Environment} using {StorageProvider} storage.",
        app.Environment.EnvironmentName, StorageProviderConfiguration.GetProvider(configuration));

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging(options =>
    {
        // Tag the request summary line with the caller, which is only known after authentication.
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var login = UserLogScopeMiddleware.GetLogin(httpContext);
            if (login != null)
            {
                diagnosticContext.Set(UserLogScopeMiddleware.UserProperty, login);
            }
        };
    });
    app.UseGraphQlStandards(); // Must be here for context.Request.EnableBuffering().
    app.UseRouting();
    app.UseCors();
    app.UseAuthentication();
    app.UseMiddleware<UserLogScopeMiddleware>();
    app.UseAuthorization();

    // Simple GitHub Access Token proxy.
    var apiJsonSerializerOptions = new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    app.MapPost("/api/oauth/access_token", async (HttpRequest request, IGitHubAuthClient authClient, ILogger<Program> logger) =>
    {
        var form = await request.ReadFormAsync();
        var accessTokenRequest = new AccessTokenRequest
        {
            GrantType = GetValue(form, "grant_type"),
            Code = GetValue(form, "code"),
            RedirectUrl = GetValue(form, "redirect_uri"),
            CodeVerifier = GetValue(form, "code_verifier"),
            ClientId = GetValue(form, "client_id"),
        };

        IResult result;
        try
        {
            var accessTokenResponse = await authClient.GetAccessTokenAsync(accessTokenRequest);
            result = Results.Ok(accessTokenResponse);
        }
        catch (HttpRequestException e)
        {
            var statusCode = (int)(e.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
            logger.LogWarning(e, "Error getting access token ({StatusCode}).", statusCode);
            result = Results.StatusCode(statusCode);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Internal server error getting access token.");
            result = Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        return result;
    })
        .WithName("OAuthAccessToken")
        .WithSummary("Exchange an OAuth code for an access token")
        .WithDescription("Accepts form-urlencoded OAuth fields. Returns 403 when the user is not an active member of an allowed organization. Upstream OAuth errors retain their status code.")
        .Produces<AccessTokenResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status500InternalServerError)
        .AllowAnonymous();

    // User info endpoint since GitHub does not provide one for OAuth.
    app.MapGet("/api/oauth/userinfo", async (HttpRequest request, IGitHubAuthClient authClient, ILogger<Program> logger) =>
        {
            IResult result;
            try
            {
                var userInfo = await authClient.GetUserInfoAsync();
                result = Results.Json(userInfo, apiJsonSerializerOptions, statusCode: StatusCodes.Status200OK);
            }
            catch (HttpRequestException e)
            {
                var statusCode = (int)(e.StatusCode ?? System.Net.HttpStatusCode.InternalServerError);
                logger.LogWarning(e, "Error getting user info ({StatusCode}).", statusCode);
                result = Results.StatusCode(statusCode);
            }

            return result;
        })
        .WithName("OAuthUserInfo")
        .WithSummary("Get the authenticated user's profile")
        .WithDescription("Returns GitHub user information. Upstream HTTP errors retain their status code.")
        .Produces<UserInfo>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

    // GitHub App events, authenticated by the webhook signature.
    app.MapGitHubWebhooks();

    app.MapGraphQL().WithOptions(options =>
    {
        options.Tool.Enable = false; // Use Apollo Sandbox instead of Banana Cake Pop.
    });

    if (configuration.GetValue<bool>("ApolloSandbox:Enabled"))
    {
        app.MapApolloSandboxRedirect();
    }

    MapHealthCheck(app, "/health/ready", "ready", "Readiness", "Check application readiness",
        "Returns 200 when the application is ready, or 503 otherwise.");
    MapHealthCheck(app, "/health/live", "live", "Liveness", "Check application liveness",
        "Returns 200 while the application is live, or 503 otherwise.");

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Error starting web host");
    throw;
}
finally
{
    Log.Debug("Cleaning up...");
    Log.CloseAndFlush();
}

// Required for integration testing.
// SourceRef: https://stackoverflow.com/questions/55131379/integration-testing-asp-net-core-with-net-framework-cant-find-deps-json
/// <summary>
/// Represents program data used by the application.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Retrieves the value.
    /// </summary>
    /// <param name="form">Object containing the form data used by the operation.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public static string GetValue(IFormCollection form, string name)
    {
        return (form?.TryGetValue(name, out var value) == true ? value.FirstOrDefault() : string.Empty)!;
    }
    /// <summary>
    /// Maps an anonymous, uncached plain-text health endpoint that runs the health checks carrying a tag.
    /// </summary>
    /// <param name="endpoints">Route builder the endpoint is added to.</param>
    /// <param name="pattern">Route of the endpoint.</param>
    /// <param name="tag">Health check tag to run, such as <c>ready</c> or <c>live</c>.</param>
    /// <param name="name">OpenAPI operation name.</param>
    /// <param name="summary">OpenAPI summary.</param>
    /// <param name="description">OpenAPI description.</param>
    private static void MapHealthCheck(IEndpointRouteBuilder endpoints, string pattern, string tag, string name,
        string summary, string description)
    {
        var options = new HealthCheckOptions { Predicate = healthCheck => healthCheck.Tags.Contains(tag) };
        endpoints.MapGet(pattern, async (HealthCheckService healthCheckService, HttpContext context) =>
            {
                var report = await healthCheckService.CheckHealthAsync(options.Predicate, context.RequestAborted);
                context.Response.Headers.CacheControl = "no-store, no-cache";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "Thu, 01 Jan 1970 00:00:00 GMT";
                return Results.Content(report.Status.ToString(), "text/plain",
                    statusCode: options.ResultStatusCodes[report.Status]);
            })
            .WithName(name)
            .WithSummary(summary)
            .WithDescription(description)
            .WithMetadata(
                new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(string), ["text/plain"]),
                new ProducesResponseTypeMetadata(StatusCodes.Status503ServiceUnavailable, typeof(string),
                    ["text/plain"]))
            .AllowAnonymous();
    }
}

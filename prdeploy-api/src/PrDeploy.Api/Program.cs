using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using PrDeploy.Api;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Builder;
using PrDeploy.Api.Business;
using PrDeploy.Api.Business.Clients.Interfaces;
using PrDeploy.Api.Configuration;
using PrDeploy.Api.Models;
using PrDeploy.Api.Models.Auth;
using PrDeploy.Api.Options;
using PrDeploy.Api.Webhooks;
using Serilog;
using Serilog.Formatting.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Path = System.IO.Path;

try
{
    var root = Directory.GetCurrentDirectory();
    var dotenv = Path.Combine(root, ".env");
    DotEnv.Load(dotenv);

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddOpenApi();

    var configuration = builder.Configuration
        // This is loaded in the Kubernetes cluster as a mounted secret.
        .AddJsonFile("secrets/appsettings.secret.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

    //initial logger without env running
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext() // Includes webhook scopes such as Owner, Repo and PullNumber.
        .WriteTo.Console(new JsonFormatter(renderMessage: true))
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

    builder.Host.UseSerilog();
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging();
    app.UseGraphQlStandards(); // Must be here for context.Request.EnableBuffering().
    app.UseRouting();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    // Simple GitHub Access Token proxy.
    var apiJsonSerializerOptions = new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    app.MapPost("/api/oauth/access_token", async (HttpRequest request, IGitHubAuthClient authClient) =>
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
            Log.Logger.Error(e, $"Error getting access token ({e.StatusCode}).");
            result = Results.StatusCode((int)e.StatusCode!);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, $"Internal Server error getting access token.");
            result = Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        return result;
    })
        .WithName("OAuthAccessToken")
        .WithSummary("Exchange an OAuth code for an access token")
        .WithDescription("Accepts form-urlencoded OAuth fields. Upstream OAuth errors retain their status code.")
        .Produces<AccessTokenResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status500InternalServerError)
        .AllowAnonymous();

    // User info endpoint since GitHub does not provide one for OAuth.
    app.MapGet("/api/oauth/userinfo", async (HttpRequest request, IGitHubAuthClient authClient) =>
        {
            IResult result;
            try
            {
                var userInfo = await authClient.GetUserInfoAsync();
                result = Results.Json(userInfo, apiJsonSerializerOptions, statusCode: StatusCodes.Status200OK);
            }
            catch (HttpRequestException e)
            {
                Log.Logger.Error(e, $"Error getting user info ({e.StatusCode}).");
                result = Results.StatusCode((int)e.StatusCode!);
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

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = healthCheck => healthCheck.Tags.Contains("ready")
    })
        .WithName("Readiness")
        .WithSummary("Check application readiness")
        .WithDescription("Returns 200 when the application is ready, or 503 otherwise.")
        .WithMetadata(
            new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(string), ["text/plain"]),
            new ProducesResponseTypeMetadata(StatusCodes.Status503ServiceUnavailable, typeof(string), ["text/plain"]))
        .AllowAnonymous();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = healthCheck => healthCheck.Tags.Contains("live")
    })
        .WithName("Liveness")
        .WithSummary("Check application liveness")
        .WithDescription("Returns 200 while the application is live, or 503 otherwise.")
        .WithMetadata(
            new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(string), ["text/plain"]),
            new ProducesResponseTypeMetadata(StatusCodes.Status503ServiceUnavailable, typeof(string), ["text/plain"]))
        .AllowAnonymous();

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
/// Represents the program used by the application.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Retrieves the Value.
    /// </summary>
    /// <param name="form">Value supplied for form.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <returns>The value produced by this operation.</returns>
    public static string GetValue(IFormCollection form, string name)
    {
        return (form?.TryGetValue(name, out var value) == true ? value.FirstOrDefault() : string.Empty)!;
    }
}

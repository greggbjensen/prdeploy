using HotChocolate.AspNetCore;
using PrDeploy.Api;
using PrDeploy.Api.Business;
using PrDeploy.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Builder;
using PrDeploy.Api.Business.Clients.Interfaces;
using PrDeploy.Api.Configuration;
using PrDeploy.Api.Models;
using PrDeploy.Api.Models.Auth;
using Serilog;
using Serilog.Formatting.Json;
using IServiceCollectionExtensions = PrDeploy.Api.IServiceCollectionExtensions;
using Path = System.IO.Path;

try
{
    var root = Directory.GetCurrentDirectory();
    var dotenv = Path.Combine(root, ".env");
    DotEnv.Load(dotenv);

    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration
        // This is loaded in the Kubernetes cluster as a mounted secret.
        .AddJsonFile("secrets/appsettings.secret.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

    //initial logger without env running
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .WriteTo.Console(new JsonFormatter(renderMessage: true))
        .CreateBootstrapLogger();

    builder.Services
        .AddPrDeployApi()
        .AddPrDeployApiBusiness(configuration)
        .AddPrDeployApiModelValidation()
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
            options.KnownNetworks.Clear();
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
    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging();
    app.UseGraphQlStandards(); // Must be here for context.Request.EnableBuffering().
    app.UseRouting();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseEndpoints(endpoints =>
    {
        // Simple GitHub Access Token proxy.
        endpoints.MapPost("/api/oauth/access_token", async (HttpRequest request, IGitHubAuthClient authClient) =>
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
        .AllowAnonymous();

        endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions {
            Tool = { Enable = false } // Use Apollo Playground instead of Banana Cake Pop.
        });
        endpoints.MapApolloSandboxRedirect();
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions {
            Predicate = healthCheck => healthCheck.Tags.Contains("ready")
        }).AllowAnonymous();
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("live")
        }).AllowAnonymous();
    });

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
public partial class Program
{
    public static string GetValue(IFormCollection form, string name)
    {
        return (form?.TryGetValue(name, out var value) == true ? value.FirstOrDefault() : string.Empty)!;
    }
}

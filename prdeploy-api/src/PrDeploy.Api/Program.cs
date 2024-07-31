using HotChocolate.AspNetCore;
using PrDeploy.Api;
using PrDeploy.Api.Business;
using PrDeploy.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Builder;
using Serilog;
using Serilog.Formatting.Json;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration
        // This is loaded in the Kubernetes cluster as a mounted secret.
        .AddJsonFile("secrets/appsettings.secret.json", optional: true, reloadOnChange: true)
        .Build();

    //initial logger without env running
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .WriteTo.Console(new JsonFormatter(renderMessage: true))
        .CreateBootstrapLogger();

    builder.Services
        .AddHttpContextAccessor()
        .AddPrDeployApi(new DeployApiOptions())
        .AddPrDeployApiBusiness(configuration)
        .AddJwtAuthentication(options => configuration.Bind("Jwt", options))
        .AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
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
    app.UseGlobalErrorHandling();
    app.UseSerilogRequestLogging();
    app.UseGraphQlStandards(); // Must be here for context.Request.EnableBuffering().
    app.UseRouting();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseEndpoints(endpoints =>
    {
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
public partial class Program { }

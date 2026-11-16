using System.Text.Encodings.Web;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Auth;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Stores;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Tests.Webhooks.Framework;

namespace PrDeploy.Api.Tests.Framework;

/// <summary>
/// Creates an isolated API test host with local storage and an in-memory deployment store.
/// </summary>
public class DeployApiApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _storageDirectory = Path.Combine(
        Path.GetTempPath(),
        "prdeploy-api-tests",
        Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Gets or sets the configure services.
    /// </summary>
    /// <value>Current configure services value.</value>
    public Action<IServiceCollection>? ConfigureServices { get; set; }

    /// <summary>
    /// Gets or sets the configure test services.
    /// </summary>
    /// <value>Current configure test services value.</value>
    public Action<IServiceCollection>? ConfigureTestServices { get; set; }

    /// <summary>
    /// Configures the test host with isolated storage and any test-specific service overrides.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            });
            services.RemoveAll<IGitHubSecurity>();
            services.AddSingleton<IGitHubSecurity, TestGitHubSecurity>();
            ConfigureTestServices?.Invoke(services);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<LiteDbStorage>();
            services.AddSingleton(_ => new LiteDbStorage(Path.Combine(_storageDirectory, "prdeploy.db")));
            services.RemoveAll<IDeploymentStore>();
            var deploymentStore = new InMemoryDeploymentStore();
            var defaultSettings = WebhookTestHost.CreateDefaultSettings();
            deploymentStore.SetAsync(WebhookTestHost.Owner, "DEPLOY_SETTINGS", defaultSettings)
                .GetAwaiter().GetResult();
            deploymentStore.SetAsync(
                    WebhookTestHost.Owner, WebhookTestHost.Repo, "DEPLOY_SETTINGS", defaultSettings)
                .GetAwaiter().GetResult();
            services.AddSingleton<IDeploymentStore>(deploymentStore);

            // Services used in all tests.
            // Adds a factory for GraphQL client to use Test Server HttpClient.
            services.AddTestHttpClientFactory(this, options =>
            {
                options.BaseAddress = new Uri("http://localhost/graphql/");
            });

            services.AddDeployApiClient();

            // Call configure services on test.
            ConfigureServices?.Invoke(services);
        });
    }

    /// <summary>
    /// Disposes the test host and removes its temporary local database directory.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_storageDirectory))
        {
            Directory.Delete(_storageDirectory, recursive: true);
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(PrDeployClaimNames.GitHubToken, "test-token"),
                new Claim(JwtRegisteredClaimNames.Name, "test-user")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class TestGitHubSecurity : IGitHubSecurity
    {
        public Task GuardRepoAsync(string owner, string repo) => Task.CompletedTask;

        public Task GuardOwnerAsync(string owner) => Task.CompletedTask;

        public Task<bool> HasRepoAccessAsync(string owner, string repo) => Task.FromResult(true);

        public Task<bool> HasOwnerAccessAsync(string owner) => Task.FromResult(true);
    }
}

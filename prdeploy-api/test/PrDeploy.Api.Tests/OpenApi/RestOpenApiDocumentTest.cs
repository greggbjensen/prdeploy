using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrDeploy.Api.Tests.Framework;
using System.Xml.Linq;

namespace PrDeploy.Api.Tests.OpenApi;

/// <summary>
/// Contains tests that verify the behavior of the rest Open Api Document Test.
/// </summary>
/// <param name="factory">Value supplied for factory.</param>
public class RestOpenApiDocumentTest(DeployApiApplicationFactory factory) : DeployApiTest(factory)
{
    /// <summary>
    /// Performs the configure Test Services operation.
    /// </summary>
    /// <param name="services">Services to include in the operation.</param>
    public override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IHostedService>();
        services.AddAuthentication("OpenApiTest")
            .AddScheme<AuthenticationSchemeOptions, NoAuthenticationHandler>("OpenApiTest", _ => { });
        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = "OpenApiTest";
            options.DefaultChallengeScheme = "OpenApiTest";
            options.DefaultScheme = "OpenApiTest";
        });
        services.PostConfigure<KeyManagementOptions>(options =>
        {
            options.XmlRepository = new InMemoryXmlRepository();
        });
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: describes Rest Endpoints.
    /// </summary>
    [Fact]
    public async Task OpenApiDocument_DescribesRestEndpoints()
    {
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        AssertEndpoint(paths, "/api/oauth/access_token", "post", "Exchange an OAuth code for an access token");
        AssertEndpoint(paths, "/api/oauth/userinfo", "get", "Get the authenticated user's profile");
        AssertEndpoint(paths, "/webhooks", "post", "Receive a GitHub App webhook");
        AssertEndpoint(paths, "/health/ready", "get", "Check application readiness");
        AssertEndpoint(paths, "/health/live", "get", "Check application liveness");
    }

    /// <summary>
    /// Performs the assert Endpoint operation.
    /// </summary>
    /// <param name="paths">Value supplied for paths.</param>
    /// <param name="path">Path used to locate the resource.</param>
    /// <param name="method">Value supplied for method.</param>
    /// <param name="summary">Value supplied for summary.</param>
    private static void AssertEndpoint(JsonElement paths, string path, string method, string summary)
    {
        Assert.True(
            paths.TryGetProperty(path, out var pathItem),
            $"OpenAPI path '{path}' not found. Paths: {string.Join(", ", paths.EnumerateObject().Select(item => item.Name))}");
        var operation = pathItem.GetProperty(method);
        Assert.Equal(summary, operation.GetProperty("summary").GetString());
        Assert.True(operation.TryGetProperty("responses", out _));
    }

    /// <summary>
    /// Represents the no Authentication Handler used by the application.
    /// </summary>
    /// <param name="options">Value supplied for options.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    /// <param name="encoder">Value supplied for encoder.</param>
    private sealed class NoAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        /// <summary>
        /// Handles the Authenticate according to the application's workflow.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }

    /// <summary>
    /// Represents the in Memory Xml Repository used by the application.
    /// </summary>
    private sealed class InMemoryXmlRepository : IXmlRepository
    {
        private readonly List<XElement> _elements = [];

        /// <summary>
        /// Retrieves the All Elements.
        /// </summary>
        /// <returns>The value produced by this operation.</returns>
        public IReadOnlyCollection<XElement> GetAllElements() =>
            _elements.Select(element => new XElement(element)).ToArray();

        /// <summary>
        /// Performs the store Element operation.
        /// </summary>
        /// <param name="element">Value supplied for element.</param>
        /// <param name="friendlyName">Value supplied for friendly Name.</param>
        public void StoreElement(XElement element, string friendlyName) =>
            _elements.Add(new XElement(element));
    }
}

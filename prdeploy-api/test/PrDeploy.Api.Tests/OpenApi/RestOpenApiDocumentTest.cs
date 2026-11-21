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

/// <summary>Contains tests that verify rest open API document behavior.</summary>
/// <param name="factory">Application factory used to start the API under test.</param>
[Collection(ApiHostCollection.Name)]
public class RestOpenApiDocumentTest(DeployApiApplicationFactory factory) : DeployApiTest(factory)
{
    /// <summary>Configures dependency injection services for the test application.</summary>
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

    /// <summary>Verifies the expected behavior for this scenario: describes rest endpoints.</summary>
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
        AssertEndpoint(paths, "/api/deploy-status/{owner}/{repo}/events", "get", "Stream deployment workflow status");
        AssertEndpoint(paths, "/health/ready", "get", "Check application readiness");
        AssertEndpoint(paths, "/health/live", "get", "Check application liveness");
    }

    /// <summary>Asserts that the endpoint.</summary>
    /// <param name="paths">Object containing the paths data used by the operation.</param>
    /// <param name="path">Path used to locate the resource.</param>
    /// <param name="method">Text used as the method.</param>
    /// <param name="summary">Text used as the summary.</param>
    private static void AssertEndpoint(JsonElement paths, string path, string method, string summary)
    {
        Assert.True(
            paths.TryGetProperty(path, out var pathItem),
            $"OpenAPI path '{path}' not found. Paths: {string.Join(", ", paths.EnumerateObject().Select(item => item.Name))}");
        var operation = pathItem.GetProperty(method);
        Assert.Equal(summary, operation.GetProperty("summary").GetString());
        Assert.True(operation.TryGetProperty("responses", out _));
    }

    /// <summary>Supplies a test authentication handler that deliberately does not authenticate requests.</summary>
    /// <param name="options">Monitor providing authentication scheme options to the base handler.</param>
    /// <param name="logger">Factory used by the base handler to create its logger.</param>
    /// <param name="encoder">Encoder used by the base handler when formatting authentication challenges.</param>
    private sealed class NoAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        /// <summary>Returns no authentication result so the OpenAPI test can inspect unauthenticated endpoints.</summary>
        /// <returns>The authenticate result produced by the operation.</returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }

    /// <summary>Stores copies of data protection key XML elements for OpenAPI tests.</summary>
    private sealed class InMemoryXmlRepository : IXmlRepository
    {
        private readonly List<XElement> _elements = [];

        /// <summary>Returns copies of all XML elements currently stored in the test repository.</summary>
        /// <returns>Requested value, or its default when no value is available.</returns>
        public IReadOnlyCollection<XElement> GetAllElements() =>
            _elements.Select(element => new XElement(element)).ToArray();

        /// <summary>Adds a copy of the supplied XML element to the test repository.</summary>
        /// <param name="element">Object containing the element data used by the operation.</param>
        /// <param name="friendlyName">Text used as the friendly name.</param>
        public void StoreElement(XElement element, string friendlyName) =>
            _elements.Add(new XElement(element));
    }
}

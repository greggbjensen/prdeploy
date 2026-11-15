using System.Text.Json;
using System.Text.Encodings.Web;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
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

public class RestOpenApiDocumentTest(DeployApiApplicationFactory factory) : DeployApiTest(factory)
{
    public override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IHostedService>();
        services.RemoveAll<IAmazonSimpleSystemsManagement>();
        var ssm = new Mock<IAmazonSimpleSystemsManagement>();
        ssm.Setup(client => client.GetParametersByPathAsync(
                It.IsAny<GetParametersByPathRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetParametersByPathResponse { Parameters = [] });
        ssm.Setup(client => client.PutParameterAsync(
                It.IsAny<PutParameterRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutParameterResponse());
        services.AddSingleton(ssm.Object);
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

    private static void AssertEndpoint(JsonElement paths, string path, string method, string summary)
    {
        Assert.True(
            paths.TryGetProperty(path, out var pathItem),
            $"OpenAPI path '{path}' not found. Paths: {string.Join(", ", paths.EnumerateObject().Select(item => item.Name))}");
        var operation = pathItem.GetProperty(method);
        Assert.Equal(summary, operation.GetProperty("summary").GetString());
        Assert.True(operation.TryGetProperty("responses", out _));
    }

    private sealed class NoAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }

    private sealed class InMemoryXmlRepository : IXmlRepository
    {
        private readonly List<XElement> _elements = [];

        public IReadOnlyCollection<XElement> GetAllElements() =>
            _elements.Select(element => new XElement(element)).ToArray();

        public void StoreElement(XElement element, string friendlyName) =>
            _elements.Add(new XElement(element));
    }
}

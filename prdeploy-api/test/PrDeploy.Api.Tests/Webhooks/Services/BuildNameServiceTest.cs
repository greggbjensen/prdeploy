using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Octokit;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Services;
using PrDeploy.Api.Webhooks.Status;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>Contains tests that verify the build name lookup used by the get-build-info action.</summary>
public class BuildNameServiceTest
{
    private readonly InMemoryDeploymentStore _store = new();
    private readonly Mock<IGitHubClient> _client = new();
    private readonly Mock<IRepositoriesClient> _repositories = new();
    private readonly BuildNameService _service;

    /// <summary>Initializes the fixture with a client that can read the repository.</summary>
    public BuildNameServiceTest()
    {
        _client.SetupGet(c => c.Repository).Returns(_repositories.Object);
        _service = new BuildNameService(_store, NullLogger<BuildNameService>.Instance);
    }

    /// <summary>Verifies mapped services are returned by workflow file name, unmapped services are skipped, and repository settings override owner settings.</summary>
    [Fact]
    public async Task ListAsync_ReturnsMappedWorkflowsWithRepoOverridingOwner()
    {
        await _store.SetAsync("o", "DEPLOY_SETTINGS", new DeploySettings
        {
            Services = [new ServiceSettings { Name = "owner-svc", Workflow = "owner-ci.yml" }]
        });
        await _store.SetAsync("o", "r", "DEPLOY_SETTINGS", new DeploySettings
        {
            Services =
            [
                new ServiceSettings { Name = "web", Workflow = ".github/workflows/web-ci.yml" },
                new ServiceSettings { Name = "api", Workflow = "api-ci.yml" },
                new ServiceSettings { Name = "no-workflow" }
            ]
        });

        var names = await _service.ListAsync(_client.Object, "o", "r");

        Assert.Equal([new BuildName("api-ci.yml", "api"), new BuildName("web-ci.yml", "web")], names);
    }

    /// <summary>Verifies a repository without settings yields an empty list instead of failing.</summary>
    [Fact]
    public async Task ListAsync_NoSettings_ReturnsEmpty()
    {
        var names = await _service.ListAsync(_client.Object, "o", "r");

        Assert.Empty(names!);
    }

    /// <summary>Verifies a token that cannot read the repository gets nothing, whatever the reason.</summary>
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task ListAsync_TokenCannotReadRepository_ReturnsNull(HttpStatusCode status)
    {
        Exception ex = status switch
        {
            HttpStatusCode.NotFound => new NotFoundException("nope", status),
            HttpStatusCode.Forbidden => new ForbiddenException(Response(status)),
            _ => new AuthorizationException(Response(status))
        };
        _repositories.Setup(r => r.Get("o", "r")).ThrowsAsync(ex);

        Assert.Null(await _service.ListAsync(_client.Object, "o", "r"));
    }

    /// <summary>Creates a GitHub response with the given status code.</summary>
    /// <param name="status">Status code the response reports.</param>
    /// <returns>A mocked response.</returns>
    private static IResponse Response(HttpStatusCode status)
    {
        var response = new Mock<IResponse>();
        response.SetupGet(r => r.StatusCode).Returns(status);
        return response.Object;
    }

    /// <summary>Verifies the bearer token is read from the Authorization header and rejected when absent or malformed.</summary>
    [Theory]
    [InlineData("Bearer abc", "abc")]
    [InlineData("bearer  abc ", "abc")]
    [InlineData("Bearer ", null)]
    [InlineData("Basic abc", null)]
    [InlineData("", null)]
    public void GetBearerToken_ParsesHeader(string header, string? expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = header;

        Assert.Equal(expected, BuildNameEndpoints.GetBearerToken(context.Request));
    }
}

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Tests.Security;

/// <summary>
/// Verifies <see cref="RepositoryService"/> guards repository access and lists only build workflow files.
/// </summary>
public class RepositoryServiceTest
{
    private const string Pattern = "/([^/]+?)-build.ya?ml";
    private readonly Mock<IGitHubClient> _client = new();
    private readonly Mock<IRepositoryContentsClient> _contents = new();
    private readonly Mock<IGitHubSecurity> _security = new();
    private readonly Mock<IDeploySettingsService> _settings = new();
    private readonly RepositoryService _service;

    /// <summary>
    /// Wires the service with a mocked GitHub contents client, security guard and merged settings.
    /// </summary>
    public RepositoryServiceTest()
    {
        var repository = new Mock<IRepositoriesClient>();
        repository.SetupGet(r => r.Content).Returns(_contents.Object);
        _client.SetupGet(c => c.Repository).Returns(repository.Object);
        SetupPattern(Pattern);
        _service = new RepositoryService(_client.Object, _security.Object, _settings.Object,
            NullLogger<RepositoryService>.Instance);
    }

    /// <summary>
    /// Stubs the merged settings with the given builds workflow pattern.
    /// </summary>
    /// <param name="pattern">Builds workflow pattern, or <c>null</c> for none.</param>
    private void SetupPattern(string? pattern) =>
        _settings.Setup(s => s.GetMergedAsync("o", "r"))
            .ReturnsAsync(new DeploySettings { Builds = new BuildsSettings { WorkflowPattern = pattern } });

    /// <summary>
    /// Stubs the workflows folder listing with the given entries.
    /// </summary>
    /// <param name="entries">Pairs of file path and content type.</param>
    private void SetupFolder(params (string Path, ContentType Type)[] entries)
    {
        IReadOnlyList<RepositoryContent> contents = entries
            .Select(e => new RepositoryContent(Path.GetFileName(e.Path), e.Path, "sha", 1, e.Type, "d", "g", "h",
                "base64", "", null, null, null))
            .ToList();
        _contents.Setup(c => c.GetAllContents("o", "r", ".github/workflows")).ReturnsAsync(contents);
    }

    /// <summary>
    /// Runs the listing for the test repository.
    /// </summary>
    /// <returns>The workflow paths returned by the service.</returns>
    private Task<List<string>> ListAsync() =>
        _service.ListWorkflowsAsync(new RepoQueryInput { Owner = "o", Repo = "r" });

    /// <summary>
    /// Confirms only files matching the builds pattern are returned, sorted and limited to YAML files.
    /// </summary>
    [Fact]
    public async Task ListWorkflowsAsync_PatternConfigured_ReturnsSortedMatchingFiles()
    {
        SetupFolder(
            (".github/workflows/web-build.yaml", ContentType.File),
            (".github/workflows/Api-BUILD.yml", ContentType.File),
            (".github/workflows/combined-deploy.yaml", ContentType.File),
            (".github/workflows/nested-build.yaml", ContentType.Dir),
            (".github/workflows/readme-build.md", ContentType.File));

        var result = await ListAsync();

        Assert.Equal([".github/workflows/Api-BUILD.yml", ".github/workflows/web-build.yaml"], result);
        _security.Verify(s => s.GuardRepoAsync("o", "r"), Times.Once);
    }

    /// <summary>
    /// Confirms every YAML file is returned when no builds pattern is configured.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("([unclosed")]
    public async Task ListWorkflowsAsync_NoUsablePattern_ReturnsAllYamlFiles(string? pattern)
    {
        SetupPattern(pattern);
        SetupFolder(
            (".github/workflows/web-build.yaml", ContentType.File),
            (".github/workflows/deploy.yml", ContentType.File),
            (".github/workflows/notes.txt", ContentType.File));

        var result = await ListAsync();

        Assert.Equal([".github/workflows/deploy.yml", ".github/workflows/web-build.yaml"], result);
    }

    /// <summary>
    /// Confirms a repository without a workflows folder yields an empty list.
    /// </summary>
    [Fact]
    public async Task ListWorkflowsAsync_NoWorkflowsFolder_ReturnsEmpty()
    {
        _contents.Setup(c => c.GetAllContents("o", "r", ".github/workflows"))
            .ThrowsAsync(new NotFoundException("missing", HttpStatusCode.NotFound));

        var result = await ListAsync();

        Assert.Empty(result);
    }

    /// <summary>
    /// Confirms access denial stops the call before GitHub is queried.
    /// </summary>
    [Fact]
    public async Task ListWorkflowsAsync_AccessDenied_ThrowsWithoutQueryingGitHub()
    {
        _security.Setup(s => s.GuardRepoAsync("o", "r"))
            .ThrowsAsync(new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden));

        await Assert.ThrowsAsync<HttpRequestException>(ListAsync);

        _contents.Verify(c => c.GetAllContents(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}

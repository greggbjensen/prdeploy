using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>
/// Contains tests that verify check service behavior.
/// </summary>
public class CheckServiceTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly CheckService _service;

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    public CheckServiceTest()
    {
        _service = _host.Get<CheckService>();
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: lists builds and versions from reference.
    /// </summary>
    [Fact]
    public async Task ListBuildsAsync_ListsBuildsAndVersionsFromReference()
    {
        _host.SetupChecks("SCRUM-16750-my-new-feature",
            ("[Bb]uild", "completed", "success", 10014663490),
            ("Lint", "completed", "success", 10014663491));
        _host.SetupBuildRun(10014663490, ".github/workflows/myorg-app-main-build.yml");

        var builds = await _service.ListBuildsAsync(GitHubModels.PullRequest(1, "SCRUM-16750-my-new-feature"));

        var build = Assert.Single(builds);
        Assert.Equal("myorg-app-main", build.Name);
        Assert.Equal("2024.2.26-r8055221191", build.Version);
        Assert.Equal(10014663490, build.RunId);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: lists builds from override run ids.
    /// </summary>
    [Fact]
    public async Task ListBuildsAsync_ListsBuildsFromOverrideRunIds()
    {
        _host.SetupBuildRun(8054571563, ".github/workflows/myorg-api-service-build.yaml");

        var builds = await _service.ListBuildsAsync(GitHubModels.PullRequest(1), [8054571563]);

        var build = Assert.Single(builds);
        Assert.Equal("myorg-api-service", build.Name);
        Assert.Equal("2024.2.26-r8055221191", build.Version);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: excludes deploy workflow checks.
    /// </summary>
    [Fact]
    public async Task ListIncompletePullChecksAsync_ExcludesDeployWorkflowChecks()
    {
        _host.SetupChecks("feature-branch",
            ("Build", "in_progress", null, 1),
            ("Deploy", "in_progress", null, 2),
            ("Tests", "completed", "failure", 3),
            ("Lint", "completed", "success", 4));
        _host.SetupBuildRun(1, ".github/workflows/app-build.yml");
        _host.SetupBuildRun(2, ".github/workflows/deploy.yml");
        _host.SetupBuildRun(3, ".github/workflows/tests.yml");

        var checks = await _service.ListIncompletePullChecksAsync(GitHubModels.PullRequest(1));

        Assert.Equal(["Build", "Tests"], checks.Select(c => c.Name));
    }

    /// <summary>
    /// Verifies a build takes the name of the service mapped to its workflow file, ignoring case and folder.
    /// </summary>
    [Fact]
    public void GetBuildName_WorkflowMappedToService_ReturnsServiceName()
    {
        var settings = new DeploySettings
        {
            Services = [new ServiceSettings { Name = "api", Path = "src/api", Workflow = "Backend-Build.yaml" }]
        };

        var name = CheckService.GetBuildName(settings, ".github/workflows/backend-build.yaml");

        Assert.Equal("api", name);
    }

    /// <summary>
    /// Verifies an unmapped workflow falls back to its file name without extension and -ci or -build suffix.
    /// </summary>
    [Theory]
    [InlineData(".github/workflows/web-build.yml", "web")]
    [InlineData(".github/workflows/web-CI.yaml", "web")]
    [InlineData(".github/workflows/web.yaml", "web")]
    [InlineData(".github/workflows/build-web.yaml", "build-web")]
    public void GetBuildName_WorkflowNotMapped_ReturnsFileNameWithoutSuffix(string path, string expected)
    {
        var settings = new DeploySettings
        {
            Services = [new ServiceSettings { Name = "other", Workflow = "other-build.yaml" }, new ServiceSettings { Name = "nowf" }]
        };

        Assert.Equal(expected, CheckService.GetBuildName(settings, path));
    }

    /// <summary>
    /// Verifies a settings object without services still produces a name.
    /// </summary>
    [Fact]
    public void GetBuildName_NoServices_ReturnsFileNameWithoutSuffix()
    {
        Assert.Equal("app", CheckService.GetBuildName(new DeploySettings { Services = null! }, ".github/workflows/app-ci.yml"));
    }

    /// <summary>
    /// Releases resources owned by this instance.
    /// </summary>
    public void Dispose() => _host.Dispose();
}

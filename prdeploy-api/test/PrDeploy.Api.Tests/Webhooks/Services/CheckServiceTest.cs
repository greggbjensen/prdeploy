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
            ("Build helm chart", "completed", "success", 10014663490),
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
    /// Releases resources owned by this instance.
    /// </summary>
    public void Dispose() => _host.Dispose();
}

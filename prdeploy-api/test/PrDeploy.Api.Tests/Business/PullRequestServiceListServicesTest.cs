using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.PullRequests.Inputs;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Tests.Business;

/// <summary>Verifies <see cref="PullRequestService.ListServicesAsync"/> reports which services a pull request triggers.</summary>
public class PullRequestServiceListServicesTest
{
    private const string HeadSha = "abc123";

    private readonly Mock<IGitHubClient> _client = new();
    private readonly Mock<IPullRequestsClient> _pullRequests = new();
    private readonly Mock<IActionsWorkflowRunsClient> _runs = new();
    private readonly Mock<IGitHubSecurity> _security = new();
    private readonly Mock<IDeploySettingsService> _settings = new();
    private readonly PullRequestService _service;

    /// <summary>Wires the service with mocked GitHub pull request and workflow run clients.</summary>
    public PullRequestServiceListServicesTest()
    {
        var workflows = new Mock<IActionsWorkflowsClient>();
        workflows.SetupGet(w => w.Runs).Returns(_runs.Object);
        var actions = new Mock<IActionsClient>();
        actions.SetupGet(a => a.Workflows).Returns(workflows.Object);
        _client.SetupGet(c => c.Actions).Returns(actions.Object);
        _client.SetupGet(c => c.PullRequest).Returns(_pullRequests.Object);

        _service = new PullRequestService(_client.Object, _security.Object, _settings.Object,
            NullLogger<PullRequestService>.Instance);
    }

    /// <summary>Stubs the merged settings with the given services.</summary>
    /// <param name="services">Services returned from the settings.</param>
    private void SetupServices(params ServiceSettings[] services) =>
        _settings.Setup(s => s.GetMergedAsync("o", "r"))
            .ReturnsAsync(new DeploySettings { Services = services.ToList() });

    /// <summary>Stubs the pull request head and the workflow runs found for its head commit.</summary>
    /// <param name="workflowPaths">Workflow paths that have a run for the head commit.</param>
    private void SetupRuns(params string[] workflowPaths)
    {
        var serializer = new Octokit.Internal.SimpleJsonSerializer();
        var pull = serializer.Deserialize<Octokit.PullRequest>($$$"""{"number":7,"head":{"sha":"{{{HeadSha}}}","ref":"branch"}}""");
        _pullRequests.Setup(p => p.Get("o", "r", 7)).ReturnsAsync(pull);

        var runs = string.Join(",", workflowPaths.Select(p => $$$"""{"path":"{{{p}}}"}"""));
        var response = serializer.Deserialize<WorkflowRunsResponse>(
            $$$"""{"total_count":{{{workflowPaths.Length}}},"workflow_runs":[{{{runs}}}]}""");
        _runs.Setup(r => r.List("o", "r", It.Is<WorkflowRunsRequest>(q => q.HeadSha == HeadSha), It.IsAny<ApiOptions>()))
            .ReturnsAsync(response);
    }

    /// <summary>Runs the listing for the test pull request.</summary>
    /// <returns>The service statuses returned by the service.</returns>
    private Task<List<Models.PullRequests.PullRequestServiceStatus>> ListAsync() =>
        _service.ListServicesAsync(new PullRequestServicesInput { Owner = "o", Repo = "r", PullNumber = 7 });

    /// <summary>Confirms services whose workflow ran are triggered, while others and unmapped services are not.</summary>
    [Fact]
    public async Task ListServicesAsync_MarksServicesWithRunsTriggered()
    {
        SetupServices(
            new ServiceSettings { Name = "api", Workflow = "api-build.yaml" },
            new ServiceSettings { Name = "app", Workflow = ".github/workflows/app-build.yaml" },
            new ServiceSettings { Name = "docs", Workflow = "docs-build.yaml" },
            new ServiceSettings { Name = "loose" });
        SetupRuns(".github/workflows/api-build.yaml", ".github/workflows/app-build.yaml");

        var result = await ListAsync();

        Assert.Equal(["api", "app", "docs", "loose"], result.Select(s => s.Name));
        Assert.Equal([true, true, false, false], result.Select(s => s.Triggered));
        _security.Verify(s => s.GuardRepoAsync("o", "r"), Times.Once);
    }

    /// <summary>Confirms workflow file names match regardless of letter case.</summary>
    [Fact]
    public async Task ListServicesAsync_MatchesWorkflowNamesIgnoringCase()
    {
        SetupServices(new ServiceSettings { Name = "api", Workflow = "API-Build.yaml" });
        SetupRuns(".github/workflows/api-build.yaml");

        var result = await ListAsync();

        Assert.True(Assert.Single(result).Triggered);
    }

    /// <summary>Confirms no services are triggered when the pull request has no workflow runs.</summary>
    [Fact]
    public async Task ListServicesAsync_NoRuns_NothingTriggered()
    {
        SetupServices(new ServiceSettings { Name = "api", Workflow = "api-build.yaml" });
        SetupRuns();

        var result = await ListAsync();

        Assert.False(Assert.Single(result).Triggered);
    }

    /// <summary>Confirms a repository without services returns an empty list without calling GitHub.</summary>
    [Fact]
    public async Task ListServicesAsync_NoServices_ReturnsEmptyWithoutCallingGitHub()
    {
        SetupServices();

        var result = await ListAsync();

        Assert.Empty(result);
        _pullRequests.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>Confirms an access failure from the security guard stops the lookup.</summary>
    [Fact]
    public async Task ListServicesAsync_GuardRejects_Throws()
    {
        _security.Setup(s => s.GuardRepoAsync("o", "r")).ThrowsAsync(new UnauthorizedAccessException());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(ListAsync);

        _settings.Verify(s => s.GetMergedAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}

using Moq;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>Contains tests that verify pull request service behavior.</summary>
public class PullRequestServiceTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly PullRequestService _service;

    /// <summary>Initializes the test fixture and prepares its dependencies.</summary>
    public PullRequestServiceTest()
    {
        _service = _host.Get<PullRequestService>();
    }

    /// <summary>Verifies the expected behavior for this scenario: gets pull request by number.</summary>
    [Fact]
    public async Task GetAsync_GetsPullRequestByNumber()
    {
        _host.MockOf(_host.GitHub.Object.PullRequest)
            .Setup(c => c.Get(WebhookTestHost.Owner, WebhookTestHost.Repo, 3097))
            .ReturnsAsync(GitHubModels.PullRequest(3097, "SCRUM-12345-my-feature", title: "Some new feature"));

        var pullRequest = await _service.GetAsync(3097);

        Assert.Equal("Some new feature", pullRequest.Title);
    }

    /// <summary>Verifies the expected behavior for this scenario: gets environment from workflow run name.</summary>
    /// <param name="runName">Text used as the run name.</param>
    /// <param name="expected">Text used as the expected.</param>
    [Theory]
    [InlineData("[stage] SCRUM-12345-my-feature: This is a test commit message.", "stage")]
    [InlineData("[prod ROLLBACK] SCRUM-12345-my-feature: This is a test commit message.", "prod")]
    [InlineData("[DEV2] SCRUM-12345-my-feature: This is a test commit message.", "dev2")]
    public void GetEnvironment_GetsEnvironmentFromWorkflowRunName(string runName, string expected)
    {
        var run = GitHubModels.WorkflowRun(1, runName);
        Assert.Equal(expected, _service.GetEnvironment(null, run));
    }

    /// <summary>Verifies the expected behavior for this scenario: gets environment from pull request when in single environment.</summary>
    [Fact]
    public void GetEnvironment_GetsEnvironmentFromPullRequestWhenInSingleEnvironment()
    {
        var pullRequest = GitHubModels.PullRequest(1, labels: ["stage", "stage-lock"]);
        Assert.Equal("stage", _service.GetEnvironment(pullRequest));
    }

    /// <summary>Verifies the expected behavior for this scenario: gets environment by priority when in multiple environments.</summary>
    [Fact]
    public void GetEnvironment_GetsEnvironmentByPriorityWhenInMultipleEnvironments()
    {
        var pullRequest = GitHubModels.PullRequest(1, labels: ["dev", "dev-lock", "stage", "stage-lock"]);
        Assert.Equal("stage", _service.GetEnvironment(pullRequest));

        pullRequest = GitHubModels.PullRequest(1,
            labels: ["dev", "dev-lock", "prod", "prod-lock", "stage", "stage-lock"]);
        Assert.Equal("prod", _service.GetEnvironment(pullRequest));
    }

    /// <summary>Verifies the expected behavior for this scenario: returns empty when no labels.</summary>
    [Fact]
    public void GetEnvironment_ReturnsEmptyWhenNoLabels()
    {
        Assert.Equal(string.Empty, _service.GetEnvironment(GitHubModels.PullRequest(1)));
    }

    /// <summary>Verifies the expected behavior for this scenario: gets rollback status from workflow run name.</summary>
    /// <param name="runName">Text used as the run name.</param>
    /// <param name="expected">Whether the expected option is enabled.</param>
    [Theory]
    [InlineData("[prod ROLLBACK] SCRUM-12345-my-feature: This is a test commit message.", true)]
    [InlineData("[prod] SCRUM-12345-my-feature: This is a test commit message.", false)]
    public void IsRollback_GetsRollbackStatusFromWorkflowRunName(string runName, bool expected)
    {
        Assert.Equal(expected, _service.IsRollback(GitHubModels.WorkflowRun(1, runName)));
    }

    /// <summary>Verifies the expected behavior for this scenario: gets count from workflow run name.</summary>
    /// <param name="runName">Text used as the run name.</param>
    /// <param name="expected">Numeric value used for the expected.</param>
    [Theory]
    [InlineData("[prod ROLLBACK] SCRUM-12345-my-feature: message.", 1)]
    [InlineData("[prod ROLLBACK 3] SCRUM-12345-my-feature: message.", 3)]
    [InlineData("[prod ROLLBACK #42 3] SCRUM-12345-my-feature: message.", 3)]
    public void GetRollbackCount_GetsCountFromWorkflowRunName(string runName, int expected)
    {
        Assert.Equal(expected, _service.GetRollbackCount(GitHubModels.WorkflowRun(1, runName)));
    }

    /// <summary>Verifies the expected behavior for this scenario: gets pull number from workflow run name.</summary>
    [Fact]
    public void GetRollbackPullNumber_GetsPullNumberFromWorkflowRunName()
    {
        Assert.Equal(42, _service.GetRollbackPullNumber(GitHubModels.WorkflowRun(1, "[prod ROLLBACK #42] x: y")));
        Assert.Equal(0, _service.GetRollbackPullNumber(GitHubModels.WorkflowRun(1, "[prod] x: y")));
    }

    /// <summary>Verifies the expected behavior for this scenario: uses assigned dev environment.</summary>
    [Fact]
    public void GetDefaultedEnvironment_UsesAssignedDevEnvironment()
    {
        var pullRequest = GitHubModels.PullRequest(1, labels: ["dev2", "dev2-lock"]);
        Assert.Equal("dev2", _service.GetDefaultedEnvironment(pullRequest, null));
        Assert.Equal("stage", _service.GetDefaultedEnvironment(pullRequest, "stage"));
        Assert.Equal("dev", _service.GetDefaultedEnvironment(GitHubModels.PullRequest(2), null));
    }

    /// <summary>Verifies the expected behavior for this scenario: lists locked environments for other pull requests.</summary>
    [Fact]
    public void GetUsedEnvironments_ListsLockedEnvironmentsForOtherPullRequests()
    {
        var pulls = new[]
        {
            GitHubModels.PullRequest(1, labels: ["dev", "dev-lock"]),
            GitHubModels.PullRequest(2, labels: ["dev2", "dev2-lock"]),
            GitHubModels.PullRequest(3, labels: ["dev3"]), // Not locked, so it can be used.
            GitHubModels.PullRequest(4, labels: ["stage", "stage-lock"])
        };

        Assert.Equal(["dev2"], _service.GetUsedEnvironments(pulls, "dev", currentPullNumber: 1));
    }

    /// <summary>Verifies the expected behavior for this scenario: removes lock labels and returns available environments.</summary>
    [Fact]
    public async Task FreeAsync_RemovesLockLabelsAndReturnsAvailableEnvironments()
    {
        string[]? labels = null;
        _host.MockOf(_host.GitHub.Object.Issue.Labels)
            .Setup(c => c.ReplaceAllForIssue(WebhookTestHost.Owner, WebhookTestHost.Repo, 5L, It.IsAny<string[]>()))
            .Callback<string, string, long, string[]>((_, _, _, l) => labels = l)
            .ReturnsAsync([]);

        var available = await _service.FreeAsync(GitHubModels.PullRequest(5, labels: ["dev2", "dev2-lock", "bug"]));

        Assert.Equal(["dev"], available);
        Assert.NotNull(labels);
        Assert.Equal(["dev2", "bug"], labels);
    }

    /// <summary>Verifies the expected behavior for this scenario: adds environment and removes other locks.</summary>
    [Fact]
    public async Task ProgressEnvironmentAsync_AddsEnvironmentAndRemovesOtherLocks()
    {
        string[]? labels = null;
        _host.MockOf(_host.GitHub.Object.Issue.Labels)
            .Setup(c => c.ReplaceAllForIssue(WebhookTestHost.Owner, WebhookTestHost.Repo, 5L, It.IsAny<string[]>()))
            .Callback<string, string, long, string[]>((_, _, _, l) => labels = l)
            .ReturnsAsync([]);

        var available = await _service.ProgressEnvironmentAsync(
            GitHubModels.PullRequest(5, labels: ["dev", "dev-lock"]), "stage", retainLocks: false);

        Assert.Equal(["dev"], available);
        Assert.NotNull(labels);
        Assert.Equal(["dev", "stage", "stage-lock"], labels);
    }

    /// <summary>Verifies the environment and branch are parsed from deploy run names, including rollbacks and names without a branch.</summary>
    /// <param name="runName">Deploy run name.</param>
    /// <param name="environment">Expected environment.</param>
    /// <param name="branch">Expected branch.</param>
    [Theory]
    [InlineData("[dev2] feature-branch: Fix the thing", "dev2", "feature-branch")]
    [InlineData("[PROD ROLLBACK #12 2] release/1.2: Release", "prod", "release/1.2")]
    [InlineData("[stage]", "stage", "")]
    [InlineData("combined-deploy", "", "")]
    [InlineData(null, "", "")]
    public void ParseDeployRunName_ParsesEnvironmentAndBranch(string? runName, string environment, string branch)
    {
        Assert.Equal((environment, branch), PullRequestService.ParseDeployRunName(runName));
    }

    /// <summary>Verifies a dispatched deploy run, which GitHub links to no pull request, resolves to the pull request on its branch labelled with its environment.</summary>
    [Fact]
    public async Task ResolveDeployPullNumberAsync_PrefersPullRequestOnRunBranch()
    {
        _host.SetupDeployedPullRequests(
            GitHubModels.PullRequest(7, "other-branch", labels: ["dev2"]),
            GitHubModels.PullRequest(8, "feature-branch", labels: ["dev2", "dev2-lock"]),
            GitHubModels.PullRequest(9, "feature-branch", labels: ["stage", "stage-lock"]));

        var pullNumber = await _service.ResolveDeployPullNumberAsync(
            GitHubModels.WorkflowRun(1, "[dev2] feature-branch: Fix"));

        Assert.Equal(8, pullNumber);
    }

    /// <summary>Verifies the only pull request labelled with the environment is used when the run name has no branch.</summary>
    [Fact]
    public async Task ResolveDeployPullNumberAsync_UsesOnlyLabelledPullRequestWithoutBranch()
    {
        _host.SetupDeployedPullRequests(GitHubModels.PullRequest(8, labels: ["stage"]));

        Assert.Equal(8, await _service.ResolveDeployPullNumberAsync(GitHubModels.WorkflowRun(1, "[stage]")));
    }

    /// <summary>Verifies no pull request is resolved when the name has no environment or several pull requests are ambiguous.</summary>
    /// <param name="runName">Deploy run name.</param>
    [Theory]
    [InlineData("combined-deploy")]
    [InlineData("[dev] unknown-branch: Fix")]
    public async Task ResolveDeployPullNumberAsync_ReturnsZeroWhenUnresolved(string runName)
    {
        _host.SetupDeployedPullRequests(
            GitHubModels.PullRequest(7, "a", labels: ["dev"]),
            GitHubModels.PullRequest(8, "b", labels: ["dev"]));

        Assert.Equal(0, await _service.ResolveDeployPullNumberAsync(GitHubModels.WorkflowRun(1, runName)));
    }

    /// <summary>Verifies changing a pull request's labels records it so its prdeploy check is refreshed.</summary>
    [Fact]
    public async Task FreeAsync_TracksPullRequestForCheckRefresh()
    {
        _host.CaptureLabels();

        await _service.FreeAsync(GitHubModels.PullRequest(8, labels: ["dev", "dev-lock"]));

        Assert.Equal([8], _host.Get<DeployCheckTracker>().PullNumbers);
    }

    /// <summary>Releases resources owned by this instance.</summary>
    public void Dispose() => _host.Dispose();
}

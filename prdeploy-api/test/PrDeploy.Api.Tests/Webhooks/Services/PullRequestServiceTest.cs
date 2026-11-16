using Moq;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>
/// Contains tests that verify the behavior of the pull Request Service Test.
/// </summary>
public class PullRequestServiceTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly PullRequestService _service;

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    public PullRequestServiceTest()
    {
        _service = _host.Get<PullRequestService>();
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: gets Pull Request By Number.
    /// </summary>
    [Fact]
    public async Task GetAsync_GetsPullRequestByNumber()
    {
        _host.MockOf(_host.GitHub.Object.PullRequest)
            .Setup(c => c.Get(WebhookTestHost.Owner, WebhookTestHost.Repo, 3097))
            .ReturnsAsync(GitHubModels.PullRequest(3097, "SCRUM-12345-my-feature", title: "Some new feature"));

        var pullRequest = await _service.GetAsync(3097);

        Assert.Equal("Some new feature", pullRequest.Title);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: gets Environment From Workflow Run Name.
    /// </summary>
    /// <param name="runName">Value supplied for run Name.</param>
    /// <param name="expected">Value supplied for expected.</param>
    [Theory]
    [InlineData("[stage] SCRUM-12345-my-feature: This is a test commit message.", "stage")]
    [InlineData("[prod ROLLBACK] SCRUM-12345-my-feature: This is a test commit message.", "prod")]
    [InlineData("[DEV2] SCRUM-12345-my-feature: This is a test commit message.", "dev2")]
    public void GetEnvironment_GetsEnvironmentFromWorkflowRunName(string runName, string expected)
    {
        var run = GitHubModels.WorkflowRun(1, runName);
        Assert.Equal(expected, _service.GetEnvironment(null, run));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: gets Environment From Pull Request When In Single Environment.
    /// </summary>
    [Fact]
    public void GetEnvironment_GetsEnvironmentFromPullRequestWhenInSingleEnvironment()
    {
        var pullRequest = GitHubModels.PullRequest(1, labels: ["stage", "stage-lock"]);
        Assert.Equal("stage", _service.GetEnvironment(pullRequest));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: gets Environment By Priority When In Multiple Environments.
    /// </summary>
    [Fact]
    public void GetEnvironment_GetsEnvironmentByPriorityWhenInMultipleEnvironments()
    {
        var pullRequest = GitHubModels.PullRequest(1, labels: ["dev", "dev-lock", "stage", "stage-lock"]);
        Assert.Equal("stage", _service.GetEnvironment(pullRequest));

        pullRequest = GitHubModels.PullRequest(1,
            labels: ["dev", "dev-lock", "prod", "prod-lock", "stage", "stage-lock"]);
        Assert.Equal("prod", _service.GetEnvironment(pullRequest));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: returns Empty When No Labels.
    /// </summary>
    [Fact]
    public void GetEnvironment_ReturnsEmptyWhenNoLabels()
    {
        Assert.Equal(string.Empty, _service.GetEnvironment(GitHubModels.PullRequest(1)));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: gets Rollback Status From Workflow Run Name.
    /// </summary>
    /// <param name="runName">Value supplied for run Name.</param>
    /// <param name="expected">Value supplied for expected.</param>
    [Theory]
    [InlineData("[prod ROLLBACK] SCRUM-12345-my-feature: This is a test commit message.", true)]
    [InlineData("[prod] SCRUM-12345-my-feature: This is a test commit message.", false)]
    public void IsRollback_GetsRollbackStatusFromWorkflowRunName(string runName, bool expected)
    {
        Assert.Equal(expected, _service.IsRollback(GitHubModels.WorkflowRun(1, runName)));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: gets Count From Workflow Run Name.
    /// </summary>
    /// <param name="runName">Value supplied for run Name.</param>
    /// <param name="expected">Value supplied for expected.</param>
    [Theory]
    [InlineData("[prod ROLLBACK] SCRUM-12345-my-feature: message.", 1)]
    [InlineData("[prod ROLLBACK 3] SCRUM-12345-my-feature: message.", 3)]
    [InlineData("[prod ROLLBACK #42 3] SCRUM-12345-my-feature: message.", 3)]
    public void GetRollbackCount_GetsCountFromWorkflowRunName(string runName, int expected)
    {
        Assert.Equal(expected, _service.GetRollbackCount(GitHubModels.WorkflowRun(1, runName)));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: gets Pull Number From Workflow Run Name.
    /// </summary>
    [Fact]
    public void GetRollbackPullNumber_GetsPullNumberFromWorkflowRunName()
    {
        Assert.Equal(42, _service.GetRollbackPullNumber(GitHubModels.WorkflowRun(1, "[prod ROLLBACK #42] x: y")));
        Assert.Equal(0, _service.GetRollbackPullNumber(GitHubModels.WorkflowRun(1, "[prod] x: y")));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: uses Assigned Dev Environment.
    /// </summary>
    [Fact]
    public void GetDefaultedEnvironment_UsesAssignedDevEnvironment()
    {
        var pullRequest = GitHubModels.PullRequest(1, labels: ["dev2", "dev2-lock"]);
        Assert.Equal("dev2", _service.GetDefaultedEnvironment(pullRequest, null));
        Assert.Equal("stage", _service.GetDefaultedEnvironment(pullRequest, "stage"));
        Assert.Equal("dev", _service.GetDefaultedEnvironment(GitHubModels.PullRequest(2), null));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: lists Locked Environments For Other Pull Requests.
    /// </summary>
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

    /// <summary>
    /// Verifies the expected behavior for this scenario: removes Lock Labels And Returns Available Environments.
    /// </summary>
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
        Assert.Equal(["dev2", "bug"], labels);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: adds Environment And Removes Other Locks.
    /// </summary>
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
        Assert.Equal(["dev", "stage", "stage-lock"], labels);
    }

    /// <summary>
    /// Releases resources owned by this instance.
    /// </summary>
    public void Dispose() => _host.Dispose();
}

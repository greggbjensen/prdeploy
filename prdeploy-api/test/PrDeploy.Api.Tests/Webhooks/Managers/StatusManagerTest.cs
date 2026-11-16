using Moq;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Managers;

namespace PrDeploy.Api.Tests.Webhooks.Managers;

/// <summary>
/// Contains tests that verify the behavior of the status Manager Test.
/// </summary>
public class StatusManagerTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly StatusManager _manager;

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    public StatusManagerTest()
    {
        _manager = _host.Get<StatusManager>();
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: matches Configured Deploy Workflow.
    /// </summary>
    /// <param name="path">Path used to locate the resource.</param>
    /// <param name="expected">Value supplied for expected.</param>
    [Theory]
    [InlineData(".github/workflows/deploy.yml", true)]
    [InlineData(".github/workflows/DEPLOY.yml", true)]
    [InlineData(".github/workflows/app-build.yml", false)]
    [InlineData(null, false)]
    public void IsDeployWorkflow_MatchesConfiguredDeployWorkflow(string? path, bool expected)
    {
        Assert.Equal(expected, _manager.IsDeployWorkflow(path));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: retriggers Deploy When Build Completes.
    /// </summary>
    [Fact]
    public async Task ProcessWorkflowRunAsync_RetriggersDeployWhenBuildCompletes()
    {
        var comments = _host.CaptureComments();
        var dispatches = _host.CaptureDispatches();
        _host.CaptureLabels();
        _host.SetupDeployedPullRequests();
        var pullRequest = GitHubModels.PullRequest(4, labels: ["stage", "stage-lock"]);
        _host.SetupPullRequest(pullRequest);
        _host.SetupChecks(pullRequest.Head.Ref, ("Build helm chart", "completed", "success", 10014663490));
        _host.SetupBuildRun(10014663490, ".github/workflows/app-build.yml");
        _host.MockOf(_host.GitHub.Object.PullRequest)
            .Setup(c => c.Commits(WebhookTestHost.Owner, WebhookTestHost.Repo, 4))
            .ReturnsAsync([GitHubModels.FromJson<PullRequestCommit>("""{ "commit": { "message": "Fix" } }""")]);

        await _manager.ProcessWorkflowRunAsync(
            GitHubModels.WorkflowRun(10014663490, "Build", ".github/workflows/app-build.yml", "success", 4),
            StatusManager.CompletedAction);

        Assert.Contains("Check%20Runs%20Complete", Assert.Single(comments).Body);
        Assert.Equal("stage", Assert.Single(dispatches).Dispatch.Inputs["environment"]);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: ignores Runs Without Pull Request.
    /// </summary>
    [Fact]
    public async Task ProcessWorkflowRunAsync_IgnoresRunsWithoutPullRequest()
    {
        _host.MockOf(_host.GitHub.Object.Actions.Workflows.Runs)
            .Setup(c => c.Get(WebhookTestHost.Owner, WebhookTestHost.Repo, 1))
            .ReturnsAsync(GitHubModels.WorkflowRun(1, "Nightly"));

        await _manager.ProcessWorkflowRunAsync(GitHubModels.WorkflowRun(1, "Nightly"), StatusManager.RequestedAction);

        _host.MockOf(_host.GitHub.Object.PullRequest)
            .Verify(c => c.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: promotes Release To Stable When Merged.
    /// </summary>
    [Fact]
    public async Task ProcessPullRequestClosedAsync_PromotesReleaseToStableWhenMerged()
    {
        _host.CaptureLabels();
        await _host.DeploymentStore.SetAsync(WebhookTestHost.Owner, WebhookTestHost.Repo, "DEPLOY_STATE_PROD",
            new PrDeploy.Api.Models.DeployEnvironments.DeployState
            {
                PullNumber = 4,
                Services = [new() { Name = "app", RunId = 222, Version = "2.0.0" }]
            });

        await _manager.ProcessPullRequestClosedAsync(
            GitHubModels.PullRequest(4, labels: ["prod", "prod-lock"], merged: true));

        var stable = await _host.DeploymentStore.GetAsync<PrDeploy.Api.Models.DeployEnvironments.DeployState>(
            WebhookTestHost.Owner, WebhookTestHost.Repo, "DEPLOY_STATE_STABLE");
        Assert.Equal(4, stable.PullNumber);
        Assert.Equal("2.0.0", Assert.Single(stable.Services!).Version);
    }

    /// <summary>
    /// Releases resources owned by this instance.
    /// </summary>
    public void Dispose() => _host.Dispose();
}

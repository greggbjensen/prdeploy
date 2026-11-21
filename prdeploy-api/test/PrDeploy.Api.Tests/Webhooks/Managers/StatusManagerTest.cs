using Moq;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Managers;

namespace PrDeploy.Api.Tests.Webhooks.Managers;

/// <summary>Contains tests that verify status manager behavior.</summary>
public class StatusManagerTest : IDisposable
{
    private readonly WebhookTestHost _host = new();
    private readonly StatusManager _manager;

    /// <summary>Initializes the test fixture and prepares its dependencies.</summary>
    public StatusManagerTest()
    {
        _manager = _host.Get<StatusManager>();
    }

    /// <summary>Verifies the expected behavior for this scenario: matches configured deploy workflow.</summary>
    /// <param name="path">Path used to locate the resource.</param>
    /// <param name="expected">Whether the expected option is enabled.</param>
    [Theory]
    [InlineData(".github/workflows/combined-deploy.yaml", true)]
    [InlineData(".github/workflows/COMBINED-DEPLOY.YAML", true)]
    [InlineData(".github/workflows/deploy.yml", false)]
    [InlineData(".github/workflows/app-build.yml", false)]
    [InlineData(null, false)]
    public void IsDeployWorkflow_MatchesConfiguredDeployWorkflow(string? path, bool expected)
    {
        Assert.Equal(expected, _manager.IsDeployWorkflow(path));
    }

    /// <summary>Verifies a deploy workflow configured as a repository path still matches runs by file name.</summary>
    [Fact]
    public void IsDeployWorkflow_MatchesDeployWorkflowConfiguredAsPath()
    {
        _host.Context.Settings.DeployWorkflow = ".github/workflows/combined-deploy.yaml";

        Assert.True(_manager.IsDeployWorkflow(".github/workflows/combined-deploy.yaml"));
        Assert.False(_manager.IsDeployWorkflow(".github/workflows/app-build.yml"));
    }

    /// <summary>Verifies the deploy and enabled automation test workflows are excluded from retriggering deploys, whether configured as file names or repository paths.</summary>
    /// <param name="deployWorkflow">Configured deploy workflow.</param>
    /// <param name="testWorkflow">Configured automation test workflow.</param>
    [Theory]
    [InlineData("combined-deploy.yaml", "e2e-tests.yml")]
    [InlineData(".github/workflows/combined-deploy.yaml", ".github/workflows/E2E-Tests.yml")]
    public void IsExcludedWorkflow_ExcludesWorkflowsPrDeployTriggers(string deployWorkflow, string testWorkflow)
    {
        _host.Context.Settings.DeployWorkflow = deployWorkflow;
        _host.Context.Settings.Environments![0].AutomationTest = new() { Enabled = true, Workflow = testWorkflow };

        Assert.True(_manager.IsExcludedWorkflow(".github/workflows/combined-deploy.yaml"));
        Assert.True(_manager.IsExcludedWorkflow(".github/workflows/e2e-tests.yml"));
        Assert.False(_manager.IsExcludedWorkflow(".github/workflows/app-build.yml"));
    }

    /// <summary>Verifies the expected behavior for this scenario: retriggers deploy when build completes.</summary>
    [Fact]
    public async Task ProcessWorkflowRunAsync_RetriggersDeployWhenBuildCompletes()
    {
        var comments = _host.CaptureComments();
        var dispatches = _host.CaptureDispatches();
        _host.CaptureLabels();
        _host.SetupDeployedPullRequests();
        var pullRequest = GitHubModels.PullRequest(4, labels: ["stage", "stage-lock"]);
        _host.SetupPullRequest(pullRequest);
        _host.SetupChecks(pullRequest.Head.Ref, ("Build details", "completed", "success", 10014663490, "PR_DEPLOY_BUILD"));
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

    /// <summary>Verifies the expected behavior for this scenario: ignores runs without pull request.</summary>
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

    /// <summary>Verifies the expected behavior for this scenario: promotes release to stable when merged.</summary>
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

    /// <summary>Releases resources owned by this instance.</summary>
    public void Dispose() => _host.Dispose();
}

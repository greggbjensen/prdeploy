using System.Net;
using Moq;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Managers;

namespace PrDeploy.Api.Tests.Webhooks.Managers;

/// <summary>Contains tests that verify prdeploy's custom deployment protection rule.</summary>
public class DeploymentProtectionManagerTest : IDisposable
{
    private const string Owner = WebhookTestHost.Owner;
    private const string Repo = WebhookTestHost.Repo;
    private const string DeployPath = ".github/workflows/combined-deploy.yaml";

    private readonly WebhookTestHost _host = new();
    private readonly DeploymentProtectionManager _manager;

    /// <summary>Initializes the test fixture.</summary>
    public DeploymentProtectionManagerTest()
    {
        _manager = _host.Get<DeploymentProtectionManager>();
    }

    /// <summary>Verifies the deploy run of the pull request holding the environment lock is approved.</summary>
    [Fact]
    public async Task DecideAsync_ApprovesLockHolderBranch()
    {
        _host.SetupDeployedPullRequests(GitHubModels.PullRequest(8, labels: ["dev2", "dev2-lock"]));

        var decision = await _manager.DecideAsync(Run("[dev2] feature-branch: Fix"), "dev2");

        Assert.True(decision.Approved);
        Assert.Contains("#8", decision.Comment);
    }

    /// <summary>Verifies prdeploy rollbacks, whose run names carry the triggering branch, are approved while the environment is locked.</summary>
    [Fact]
    public async Task DecideAsync_ApprovesRollback()
    {
        _host.SetupDeployedPullRequests(GitHubModels.PullRequest(4, "release-branch", labels: ["prod", "prod-lock"]));

        var decision = await _manager.DecideAsync(Run("[prod ROLLBACK #8] feature-branch: Fix"), "prod");

        Assert.True(decision.Approved);
    }

    /// <summary>Verifies other workflows that use the environment are approved, since prdeploy only gates its deploy workflow.</summary>
    [Fact]
    public async Task DecideAsync_ApprovesOtherWorkflows()
    {
        var decision = await _manager.DecideAsync(Run("Nightly", ".github/workflows/e2e.yml"), "dev");

        Assert.True(decision.Approved);
    }

    /// <summary>Verifies deploy runs are rejected when nothing holds the lock, another branch holds it, the lock is ambiguous, or the run was not started by prdeploy.</summary>
    /// <param name="runName">Deploy run name.</param>
    /// <param name="lockHolders">Branch and labels of each pull request with environment labels.</param>
    /// <param name="expectedComment">Text the rejection comment contains.</param>
    [Theory]
    [InlineData("[dev] feature-branch: Fix", "", "No pull request holds the dev lock")]
    [InlineData("[dev] feature-branch: Fix", "other-branch", "locked by #1")]
    [InlineData("[dev] feature-branch: Fix", "feature-branch,other-branch", "more than one pull request")]
    [InlineData("Manual deploy", "feature-branch", "not started by prdeploy")]
    [InlineData("[stage] feature-branch: Fix", "feature-branch", "not started by prdeploy")]
    public async Task DecideAsync_RejectsUnlockedDeploys(string runName, string lockHolders, string expectedComment)
    {
        var pulls = lockHolders.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select((branch, i) => GitHubModels.PullRequest(i + 1, branch, labels: ["dev", "dev-lock"]))
            .ToArray();
        _host.SetupDeployedPullRequests(pulls);

        var decision = await _manager.DecideAsync(Run(runName), "dev");

        Assert.False(decision.Approved);
        Assert.Contains(expectedComment, decision.Comment);
    }

    /// <summary>Verifies the decision is posted to the run's protection rule review endpoint for the environment.</summary>
    [Fact]
    public async Task ProcessAsync_PostsDecisionForRun()
    {
        _host.SetupDeployedPullRequests();
        _host.MockOf(_host.GitHub.Object.Actions.Workflows.Runs)
            .Setup(c => c.Get(Owner, Repo, 5001))
            .ReturnsAsync(Run("[dev] feature-branch: Fix"));
        Uri? postedUri = null;
        object? postedBody = null;
        _host.MockOf(_host.GitHub.Object.Connection)
            .Setup(c => c.Post(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Uri, object, string, CancellationToken>((uri, body, _, _) => (postedUri, postedBody) = (uri, body))
            .ReturnsAsync(HttpStatusCode.NoContent);

        await _manager.ProcessAsync("dev",
            $"https://api.github.com/repos/{Owner}/{Repo}/actions/runs/5001/deployment_protection_rule");

        Assert.Equal($"repos/{Owner}/{Repo}/actions/runs/5001/deployment_protection_rule", postedUri!.ToString());
        var json = System.Text.Json.JsonSerializer.Serialize(postedBody);
        Assert.Contains("\"environment_name\":\"dev\"", json);
        Assert.Contains("\"state\":\"rejected\"", json);
    }

    /// <summary>Verifies nothing is posted when the callback URL holds no run ID.</summary>
    [Fact]
    public async Task ProcessAsync_IgnoresUnknownCallback()
    {
        await _manager.ProcessAsync("dev", "https://example.com/elsewhere");

        _host.MockOf(_host.GitHub.Object.Connection).Verify(
            c => c.Post(It.IsAny<Uri>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Builds a workflow run of the deploy workflow, or of another workflow.</summary>
    /// <param name="name">Run name.</param>
    /// <param name="path">Workflow file path.</param>
    /// <returns>The workflow run.</returns>
    private static WorkflowRun Run(string name, string path = DeployPath) => GitHubModels.WorkflowRun(5001, name, path);

    /// <summary>Releases resources owned by this instance.</summary>
    public void Dispose() => _host.Dispose();
}

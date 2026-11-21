using Moq;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Managers;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Managers;

/// <summary>Contains tests that verify the buttons on the prdeploy check run.</summary>
public class CheckRunActionManagerTest : IDisposable
{
    private const string Owner = WebhookTestHost.Owner;
    private const string Repo = WebhookTestHost.Repo;

    private readonly WebhookTestHost _host = new();
    private readonly CheckRunActionManager _manager;
    private readonly Dictionary<long, string[]> _labels;

    /// <summary>Initializes the test fixture with a writer and captured labels.</summary>
    public CheckRunActionManagerTest()
    {
        _manager = _host.Get<CheckRunActionManager>();
        _labels = _host.CaptureLabels();
        _host.SetupCollaboratorPermission("writer", "write");
        _host.SetupCollaboratorPermission("reader", "read");
        _host.SetupDeployedPullRequests();
    }

    /// <summary>Verifies the free button releases the pull request's locks, without reacting to a comment, and refreshes its check.</summary>
    [Fact]
    public async Task ProcessRequestedActionAsync_FreeReleasesLocks()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(8, labels: ["dev", "dev-lock"]));

        await _manager.ProcessRequestedActionAsync(DeployCheckService.CheckName, DeployCheckService.FreeAction, "8", [],
            "writer");

        Assert.Equal(["dev"], _labels[8]);
        _host.MockOf(_host.GitHub.Object.Reaction.IssueComment).Verify(
            c => c.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<NewReaction>()),
            Times.Never);
        Assert.Contains(8, _host.Get<DeployCheckTracker>().PullNumbers);
    }

    /// <summary>Verifies the pull request linked by GitHub is used when the check run has no external ID.</summary>
    [Fact]
    public async Task ProcessRequestedActionAsync_FallsBackToLinkedPullRequest()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(9, labels: ["stage", "stage-lock"]));

        await _manager.ProcessRequestedActionAsync(DeployCheckService.CheckName, DeployCheckService.FreeAction, null,
            [9], "writer");

        Assert.Equal(["stage"], _labels[9]);
    }

    /// <summary>Verifies the deploy button queues or dispatches a deploy to the default environment.</summary>
    [Fact]
    public async Task ProcessRequestedActionAsync_DeployDispatchesDefaultEnvironment()
    {
        var dispatches = _host.CaptureDispatches();
        var pullRequest = GitHubModels.PullRequest(8);
        _host.SetupPullRequest(pullRequest);
        _host.SetupChecks(pullRequest.Head.Ref,
            ("Build details", "completed", "success", 10014663490, "PR_DEPLOY_BUILD"));
        _host.SetupBuildRun(10014663490, ".github/workflows/app-build.yml");
        _host.MockOf(_host.GitHub.Object.PullRequest)
            .Setup(c => c.Commits(Owner, Repo, 8))
            .ReturnsAsync([GitHubModels.FromJson<PullRequestCommit>("""{ "commit": { "message": "Fix" } }""")]);

        await _manager.ProcessRequestedActionAsync(DeployCheckService.CheckName, DeployCheckService.DeployAction, "8",
            [], "writer");

        Assert.Equal("dev", Assert.Single(dispatches).Dispatch.Inputs["environment"]);
    }

    /// <summary>Verifies buttons are refused for users without write access, buttons on other checks, unknown buttons, and deploys of forks.</summary>
    /// <param name="checkName">Check run name.</param>
    /// <param name="identifier">Button identifier.</param>
    /// <param name="sender">Login of the user who pressed it.</param>
    /// <param name="headRepository">Repository holding the pull request branch.</param>
    [Theory]
    [InlineData("prdeploy", "free", "reader", $"{Owner}/{Repo}")]
    [InlineData("prdeploy", "free", null, $"{Owner}/{Repo}")]
    [InlineData("other-check", "free", "writer", $"{Owner}/{Repo}")]
    [InlineData("prdeploy", "unknown", "writer", $"{Owner}/{Repo}")]
    [InlineData("prdeploy", "deploy", "writer", "someone/fork")]
    public async Task ProcessRequestedActionAsync_RefusesDisallowedActions(string checkName, string identifier,
        string? sender, string headRepository)
    {
        var dispatches = _host.CaptureDispatches();
        _host.SetupPullRequest(GitHubModels.PullRequest(8, labels: ["dev", "dev-lock"], headRepository: headRepository));

        await _manager.ProcessRequestedActionAsync(checkName, identifier, "8", [], sender);

        Assert.Empty(_labels);
        Assert.Empty(dispatches);
    }

    /// <summary>Releases resources owned by this instance.</summary>
    public void Dispose() => _host.Dispose();
}

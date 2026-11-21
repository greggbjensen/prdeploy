using Moq;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Services;
using PrDeploy.Api.Webhooks.Status;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>Contains tests that verify the prdeploy check run published on pull requests.</summary>
public class DeployCheckServiceTest : IDisposable
{
    private const string Owner = WebhookTestHost.Owner;
    private const string Repo = WebhookTestHost.Repo;
    private static readonly IReadOnlyDictionary<string, (int, int)> NoQueues = new Dictionary<string, (int, int)>();

    private readonly WebhookTestHost _host = new();
    private readonly DeployCheckService _service;
    private readonly List<NewCheckRun> _created = [];
    private readonly List<(long Id, CheckRunUpdate Update)> _updated = [];

    /// <summary>Initializes the test fixture and captures created and updated check runs.</summary>
    public DeployCheckServiceTest()
    {
        _service = _host.Get<DeployCheckService>();
        var checkRuns = _host.MockOf(_host.GitHub.Object.Check.Run);
        checkRuns.Setup(c => c.Create(Owner, Repo, It.IsAny<NewCheckRun>()))
            .Callback<string, string, NewCheckRun>((_, _, r) => _created.Add(r))
            .ReturnsAsync((CheckRun)null!);
        checkRuns.Setup(c => c.Update(Owner, Repo, It.IsAny<long>(), It.IsAny<CheckRunUpdate>()))
            .Callback<string, string, long, CheckRunUpdate>((_, _, id, u) => _updated.Add((id, u)))
            .ReturnsAsync((CheckRun)null!);
        SetupExistingChecks();
    }

    /// <summary>Verifies a pull request in no environment or queue gets a neutral check with a deploy button.</summary>
    [Fact]
    public void BuildView_NotDeployedOffersDeploy()
    {
        var view = _service.BuildView(GitHubModels.PullRequest(8), NoQueues, []);

        Assert.Equal(CheckStatus.Completed, view.Status);
        Assert.Equal(CheckConclusion.Neutral, view.Conclusion);
        Assert.Equal("Not deployed", view.Title);
        Assert.Contains("not deployed to any environment", view.Summary);
        var action = Assert.Single(view.Actions);
        Assert.Equal(DeployCheckService.DeployAction, action.Identifier);
        Assert.Equal("Deploy to dev", action.Label);
    }

    /// <summary>Verifies the locked environment's latest deploy status drives the check status and conclusion.</summary>
    /// <param name="state">Latest deploy workflow state of the environment.</param>
    /// <param name="status">Expected check status.</param>
    /// <param name="conclusion">Expected conclusion, or null when still running.</param>
    /// <param name="title">Expected title.</param>
    [Theory]
    [InlineData(DeployWorkflowState.InProgress, CheckStatus.InProgress, null, "Deploying to dev2")]
    [InlineData(DeployWorkflowState.Successful, CheckStatus.Completed, CheckConclusion.Success, "Deployed to dev2")]
    [InlineData(DeployWorkflowState.Failure, CheckStatus.Completed, CheckConclusion.Failure, "Deploy to dev2 failed")]
    public void BuildView_LockedEnvironmentUsesDeployStatus(DeployWorkflowState state, CheckStatus status,
        CheckConclusion? conclusion, string title)
    {
        PublishStatus("dev2", state, pullNumber: 8);

        var view = _service.BuildView(GitHubModels.PullRequest(8, labels: ["dev2", "dev2-lock"]), NoQueues, []);

        Assert.Equal(status, view.Status);
        Assert.Equal(conclusion, view.Conclusion);
        Assert.Equal(title, view.Title);
        Assert.Contains("| `dev2` | 🔒 Locked |", view.Summary);
        Assert.Equal([DeployCheckService.RedeployAction, DeployCheckService.FreeAction],
            view.Actions.Select(a => a.Identifier));
    }

    /// <summary>Verifies another pull request's status for the same environment is ignored, so the check is neutral.</summary>
    [Fact]
    public void BuildView_IgnoresOtherPullRequestStatus()
    {
        PublishStatus("dev2", DeployWorkflowState.Failure, pullNumber: 99);

        var view = _service.BuildView(GitHubModels.PullRequest(8, labels: ["dev2", "dev2-lock"]), NoQueues, []);

        Assert.Equal(CheckConclusion.Neutral, view.Conclusion);
        Assert.Equal("Holding dev2", view.Title);
    }

    /// <summary>Verifies a queued pull request gets a queued check with its position and a button to leave the queue.</summary>
    [Fact]
    public void BuildView_QueuedShowsPosition()
    {
        var view = _service.BuildView(GitHubModels.PullRequest(8),
            new Dictionary<string, (int, int)> { ["dev"] = (2, 3) }, []);

        Assert.Equal(CheckStatus.Queued, view.Status);
        Assert.Null(view.Conclusion);
        Assert.Equal("Queued for dev (2 of 3)", view.Title);
        Assert.Contains("Position **2** of 3", view.Summary);
        Assert.Contains("https://prdeploy.myorg.com/greggbjensen/prdeploy-example-repo/deployments?environment=dev",
            view.Summary);
        Assert.Equal("Leave queue", Assert.Single(view.Actions).Label);
    }

    /// <summary>Verifies a pull request from a fork gets no deploy buttons, since forks cannot be deployed.</summary>
    [Fact]
    public void BuildView_ForkHasNoDeployButtons()
    {
        Assert.Empty(_service.BuildView(GitHubModels.PullRequest(8, headRepository: "someone/fork"), NoQueues, [])
            .Actions);
        Assert.Equal([DeployCheckService.FreeAction], _service.BuildView(
                GitHubModels.PullRequest(8, labels: ["dev", "dev-lock"], headRepository: "someone/fork"), NoQueues, [])
            .Actions.Select(a => a.Identifier));
    }

    /// <summary>Verifies the summary lists the configured services and whether the pull request built them.</summary>
    [Fact]
    public void BuildView_ListsServices()
    {
        var view = _service.BuildView(GitHubModels.PullRequest(8), NoQueues, [("web", false), ("api", true)]);

        Assert.Contains("| `api` | ✅ Yes |\n| `web` | — |", view.Summary.Replace("\r", ""));
    }

    /// <summary>Verifies the check is created on the head commit with the pull request as its external ID when none exists.</summary>
    [Fact]
    public async Task PublishAsync_CreatesCheckOnHeadCommit()
    {
        await _service.PublishAsync(GitHubModels.PullRequest(8));

        var created = Assert.Single(_created);
        Assert.Equal(DeployCheckService.CheckName, created.Name);
        Assert.Equal(GitHubModels.HeadSha("feature-branch"), created.HeadSha);
        Assert.Equal("8", created.ExternalId);
        Assert.Equal("Not deployed", created.Output.Title);
        Assert.NotNull(created.CompletedAt);
        Assert.Equal("https://prdeploy.myorg.com/greggbjensen/prdeploy-example-repo/deployments", created.DetailsUrl);
        Assert.Empty(_updated);
    }

    /// <summary>Verifies an existing check that is still running is updated in place.</summary>
    [Fact]
    public async Task PublishAsync_UpdatesRunningCheck()
    {
        SetupExistingChecks((55, "in_progress"));
        PublishStatus("dev2", DeployWorkflowState.Successful, pullNumber: 8);

        await _service.PublishAsync(GitHubModels.PullRequest(8, labels: ["dev2", "dev2-lock"]));

        var (id, update) = Assert.Single(_updated);
        Assert.Equal(55, id);
        Assert.Equal(CheckConclusion.Success, update.Conclusion!.Value.Value);
        Assert.Empty(_created);
    }

    /// <summary>Verifies a completed check is replaced by a new one when the pull request starts deploying again, because GitHub cannot reopen it.</summary>
    [Fact]
    public async Task PublishAsync_CreatesNewCheckWhenCompletedCheckReopens()
    {
        SetupExistingChecks((55, "completed"));
        PublishStatus("dev2", DeployWorkflowState.InProgress, pullNumber: 8);

        await _service.PublishAsync(GitHubModels.PullRequest(8, labels: ["dev2", "dev2-lock"]));

        Assert.Equal(CheckStatus.InProgress, Assert.Single(_created).Status!.Value.Value);
        Assert.Empty(_updated);
    }

    /// <summary>Verifies each tracked pull request is read again and published once, and a GitHub error on one does not stop the others.</summary>
    [Fact]
    public async Task PublishTrackedAsync_PublishesEachTrackedPullRequestOnce()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(8));
        _host.MockOf(_host.GitHub.Object.PullRequest)
            .Setup(c => c.Get(Owner, Repo, 7))
            .ThrowsAsync(new ForbiddenException(new Mock<IResponse>().Object));
        var tracker = _host.Get<DeployCheckTracker>();
        tracker.TrackAll([8, 7, 8]);

        await _service.PublishTrackedAsync();
        await _service.PublishTrackedAsync();

        Assert.Equal("8", Assert.Single(_created).ExternalId);
        Assert.Empty(tracker.PullNumbers);
    }

    /// <summary>Records a deploy workflow status as the dashboard stream would.</summary>
    /// <param name="environment">Environment of the status.</param>
    /// <param name="state">Workflow state.</param>
    /// <param name="pullNumber">Pull request the status belongs to.</param>
    private void PublishStatus(string environment, DeployWorkflowState state, int pullNumber)
    {
        _host.Get<DeployStatusBroadcaster>().Publish(new DeployWorkflowStatus(Owner, Repo, environment, state,
            pullNumber, 5001, $"{WebhookTestHost.RepositoryUrl}/actions/runs/5001", DateTimeOffset.UnixEpoch));
    }

    /// <summary>Returns the given prdeploy check runs for the pull request head commit.</summary>
    /// <param name="checks">Check run IDs and statuses to return.</param>
    private void SetupExistingChecks(params (long Id, string Status)[] checks)
    {
        var json = string.Join(",", checks.Select(c =>
            $$"""{ "id": {{c.Id}}, "name": "prdeploy", "status": "{{c.Status}}", "app": { "id": 1 } }"""));
        _host.MockOf(_host.GitHub.Object.Check.Run)
            .Setup(c => c.GetAllForReference(Owner, Repo, GitHubModels.HeadSha("feature-branch"),
                It.IsAny<CheckRunRequest>(), It.IsAny<ApiOptions>()))
            .ReturnsAsync(GitHubModels.FromJson<CheckRunsResponse>(
                $$"""{ "total_count": {{checks.Length}}, "check_runs": [{{json}}] }"""));
    }

    /// <summary>Releases resources owned by this instance.</summary>
    public void Dispose() => _host.Dispose();
}

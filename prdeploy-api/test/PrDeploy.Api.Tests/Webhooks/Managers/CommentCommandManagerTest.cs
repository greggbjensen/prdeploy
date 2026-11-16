using Moq;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Managers;

namespace PrDeploy.Api.Tests.Webhooks.Managers;

/// <summary>
/// Contains tests that verify comment command manager behavior.
/// </summary>
public class CommentCommandManagerTest : IDisposable
{
    private const long CommentId = 2015899564;
    private const int PullNumber = 8;
    private const string Branch = "add-service-pr";
    private static readonly string IssueUrl = $"https://api.github.com/repos/greggbjensen/prdeploy-example-repo/issues/{PullNumber}";

    private readonly WebhookTestHost _host = new();
    private readonly CommentCommandManager _manager;
    private readonly List<ReactionType> _reactions = [];
    private readonly List<(string Workflow, CreateWorkflowDispatch Dispatch)> _dispatches;
    private readonly List<(long Number, string Body)> _comments;
    private readonly Dictionary<long, string[]> _labels;

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    public CommentCommandManagerTest()
    {
        _manager = _host.Get<CommentCommandManager>();
        _dispatches = _host.CaptureDispatches();
        _comments = _host.CaptureComments();
        _labels = _host.CaptureLabels();
        _host.SetupDeployedPullRequests();
        _host.SetupChecks(Branch, ("Build helm chart", "completed", "success", 10014663490));
        _host.SetupBuildRun(10014663490, ".github/workflows/deploy-app-test-build.yml");
        _host.SetupUser("greggbjensen");

        var github = _host.GitHub.Object;
        _host.MockOf(github.Reaction.IssueComment)
            .Setup(c => c.Create(WebhookTestHost.Owner, WebhookTestHost.Repo, CommentId, It.IsAny<NewReaction>()))
            .Callback<string, string, long, NewReaction>((_, _, _, r) => _reactions.Add(r.Content))
            .ReturnsAsync((Reaction)null!);

        _host.MockOf(github.PullRequest)
            .Setup(c => c.Commits(WebhookTestHost.Owner, WebhookTestHost.Repo, It.IsAny<int>()))
            .ReturnsAsync([GitHubModels.FromJson<PullRequestCommit>(
                """{ "commit": { "message": "This is a test commit message.\n\nWith more details." } }""")]);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: deploys to default environment.
    /// </summary>
    [Fact]
    public async Task ProcessCommentAsync_DeploysToDefaultEnvironment()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(PullNumber, Branch, labels: ["prod", "prod-lock"]));

        await _manager.ProcessCommentAsync(CommentId, "/deploy", IssueUrl);

        Assert.Equal([ReactionType.Rocket], _reactions);
        var (workflow, dispatch) = Assert.Single(_dispatches);
        Assert.Equal("deploy.yml", workflow);
        Assert.Equal(Branch, dispatch.Ref);
        Assert.Equal(PullNumber.ToString(), dispatch.Inputs["pull_number"]);
        Assert.Equal("dev", dispatch.Inputs["environment"]);
        Assert.Equal("[dev] add-service-pr: This is a test commit message.", dispatch.Inputs["deploy_name"]);
        Assert.Equal("10014663490", dispatch.Inputs["deploy_run_ids"]);

        // Moving to dev releases the prod lock.
        Assert.Equal(["prod", "dev", "dev-lock"], _labels[PullNumber]);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: uses next available environment in queue.
    /// </summary>
    [Fact]
    public async Task ProcessCommentAsync_UsesNextAvailableEnvironmentInQueue()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(PullNumber, Branch));
        _host.SetupDeployedPullRequests(
            GitHubModels.PullRequest(1, labels: ["dev", "dev-lock"]),
            GitHubModels.PullRequest(2, labels: ["dev2", "dev2-lock"]));

        await _manager.ProcessCommentAsync(CommentId, "Ready to test\n/deploy", IssueUrl);

        Assert.Equal("dev3", Assert.Single(_dispatches).Dispatch.Inputs["environment"]);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: enqueues when no environment is available.
    /// </summary>
    [Fact]
    public async Task ProcessCommentAsync_EnqueuesWhenNoEnvironmentIsAvailable()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(PullNumber, Branch));
        _host.SetupDeployedPullRequests(
            GitHubModels.PullRequest(1, labels: ["stage", "stage-lock"]));
        await _host.DeploymentStore.SetAsync(WebhookTestHost.Owner, WebhookTestHost.Repo, "STAGE_PR_QUEUE", new List<int> { 5 });

        await _manager.ProcessCommentAsync(CommentId, "/deploy stage", IssueUrl);

        Assert.Empty(_dispatches);
        var queue = await _host.DeploymentStore.GetAsync<List<int>>(WebhookTestHost.Owner, WebhookTestHost.Repo, "STAGE_PR_QUEUE");
        Assert.Equal([5, PullNumber], queue);
        var (_, body) = Assert.Single(_comments);
        Assert.Contains("Your position in the queue is 2.", body);
        // Same as handlebars.js, = is HTML escaped, which GitHub markdown decodes in links.
        Assert.Contains("| [stage queue](https://prdeploy.myorg.com/greggbjensen/prdeploy-example-repo/deployments?environment&#x3D;stage) |", body);
        Assert.Contains("| [5](https://github.com/greggbjensen/prdeploy-example-repo/pull/5) | [8](", body);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: force deploys with retained locks.
    /// </summary>
    [Fact]
    public async Task ProcessCommentAsync_ForceDeploysWithRetainedLocks()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(PullNumber, Branch, labels: ["dev", "dev-lock"]));
        _host.SetupDeployedPullRequests(GitHubModels.PullRequest(1, labels: ["stage", "stage-lock"]));

        await _manager.ProcessCommentAsync(CommentId, "/deploy stage --force --retain", IssueUrl);

        Assert.Equal("stage", Assert.Single(_dispatches).Dispatch.Inputs["environment"]);
        Assert.Equal(["dev", "dev-lock", "stage", "stage-lock"], _labels[PullNumber]);
        Assert.Empty(_labels[1]); // Other pull request is removed from stage.
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: frees environment and dequeues next pull request.
    /// </summary>
    [Fact]
    public async Task ProcessCommentAsync_FreesEnvironmentAndDequeuesNextPullRequest()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(PullNumber, Branch, labels: ["stage", "stage-lock"]));
        var next = GitHubModels.PullRequest(9, "next-branch");
        _host.SetupPullRequest(next);
        _host.SetupChecks("next-branch", ("Build helm chart", "completed", "success", 10014663490));
        await _host.DeploymentStore.SetAsync(WebhookTestHost.Owner, WebhookTestHost.Repo, "STAGE_PR_QUEUE", new List<int> { 9 });

        await _manager.ProcessCommentAsync(CommentId, "/free stage", IssueUrl);

        Assert.Equal([ReactionType.Eyes], _reactions);
        Assert.Equal(["stage"], _labels[PullNumber]);
        var (_, dispatch) = Assert.Single(_dispatches);
        Assert.Equal("9", dispatch.Inputs["pull_number"]);
        Assert.Equal("stage", dispatch.Inputs["environment"]);
        var queue = await _host.DeploymentStore.GetAsync<List<int>?>(WebhookTestHost.Owner, WebhookTestHost.Repo, "STAGE_PR_QUEUE");
        Assert.Empty(queue ?? []);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: rolls back to previous stable state.
    /// </summary>
    [Fact]
    public async Task ProcessCommentAsync_RollsBackToPreviousStableState()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(PullNumber, Branch, labels: ["prod", "prod-lock"]));
        var stable = GitHubModels.PullRequest(7, "stable-branch");
        _host.SetupPullRequest(stable);
        await _host.DeploymentStore.SetAsync(WebhookTestHost.Owner, WebhookTestHost.Repo, "DEPLOY_STATE_STABLE",
            new PrDeploy.Api.Models.DeployEnvironments.DeployState
            {
                PullNumber = 7,
                Services = [new() { Name = "app", RunId = 111, Version = "1.0.0" }]
            });

        await _manager.ProcessCommentAsync(CommentId, "/rollback", IssueUrl);

        Assert.Equal([ReactionType.Confused], _reactions);
        var (_, dispatch) = Assert.Single(_dispatches);
        Assert.Equal("7", dispatch.Inputs["pull_number"]);
        Assert.Equal("111", dispatch.Inputs["deploy_run_ids"]);
        Assert.Equal("[prod ROLLBACK #8] add-service-pr: This is a test commit message.", dispatch.Inputs["deploy_name"]);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: adds service deploy files.
    /// </summary>
    [Fact]
    public async Task ProcessCommentAsync_AddsServiceDeployFiles()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(PullNumber, Branch));
        var contents = _host.MockOf(_host.GitHub.Object.Repository.Content);
        contents.Setup(c => c.GetAllContentsByRef(WebhookTestHost.Owner, WebhookTestHost.Repo, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new NotFoundException("Not found", System.Net.HttpStatusCode.NotFound));

        await _manager.ProcessCommentAsync(CommentId, "/add deploy-api-test   deploy-app-test", IssueUrl);

        Assert.Equal([ReactionType.Plus1], _reactions);
        contents.Verify(c => c.CreateFile(WebhookTestHost.Owner, WebhookTestHost.Repo, "services/deploy-api-test/.deploy",
            It.Is<CreateFileRequest>(r => r.Branch == Branch && r.Message == "Adding deploy-api-test to pull request build.")));
        contents.Verify(c => c.CreateFile(WebhookTestHost.Owner, WebhookTestHost.Repo, "deploy-app-test/.deploy",
            It.IsAny<CreateFileRequest>()));
        Assert.Empty(_comments);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: comments when service is not configured.
    /// </summary>
    [Fact]
    public async Task ProcessCommentAsync_CommentsWhenServiceIsNotConfigured()
    {
        _host.SetupPullRequest(GitHubModels.PullRequest(PullNumber, Branch));

        await _manager.ProcessCommentAsync(CommentId, "/add deploy-bad-test", IssueUrl);

        var (_, body) = Assert.Single(_comments);
        Assert.Contains("- deploy-bad-test", body);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: ignores comments without commands.
    /// </summary>
    [Fact]
    public async Task ProcessCommentAsync_IgnoresCommentsWithoutCommands()
    {
        await _manager.ProcessCommentAsync(CommentId, "See https://prdeploy.myorg.com/o/r/deployments", IssueUrl);

        Assert.Empty(_reactions);
        _host.MockOf(_host.GitHub.Object.PullRequest).Verify(c => c.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// Releases resources owned by this instance.
    /// </summary>
    public void Dispose() => _host.Dispose();
}

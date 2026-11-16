using Moq;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>
/// Contains tests that verify the behavior of the status Service Test.
/// </summary>
public class StatusServiceTest : IDisposable
{
    private const int PullNumber = 3097;
    private readonly WebhookTestHost _host = new();
    private readonly StatusService _service;
    private readonly List<(long Number, string Body)> _comments;

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    public StatusServiceTest()
    {
        _service = _host.Get<StatusService>();
        _comments = _host.CaptureComments();
        _host.SetupUser("greggbjensen", "Gregg Jensen");
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: posts Pull Request Comment For Deployment Started.
    /// </summary>
    [Fact]
    public async Task PostDeployStartedAsync_PostsPullRequestCommentForDeploymentStarted()
    {
        var run = GitHubModels.WorkflowRun(7065200437, "[dev] My new feature");
        SetupRun(run);

        await _service.PostDeployStartedAsync(
            GitHubModels.PullRequest(PullNumber, labels: ["dev", "dev-lock"]), run);

        var (number, body) = Assert.Single(_comments);
        Assert.Equal(PullNumber, number);
        Assert.Equal(
            "[![dev](https://badgen.net/badge/dev/Deploy%20Started/0080ff?labelColor=d4ac0d&icon=github&scale=1.2)](https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/7065200437/attempts/1 'Open the deploy')\n",
            body);
        Assert.Empty(_host.Http.Requests); // No approval required for dev.
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: posts Pull Request Comment For Rollback Started.
    /// </summary>
    [Fact]
    public async Task PostDeployStartedAsync_PostsPullRequestCommentForRollbackStarted()
    {
        var run = GitHubModels.WorkflowRun(7065200437, "[prod ROLLBACK] My new feature");
        SetupRun(run);

        await _service.PostDeployStartedAsync(
            GitHubModels.PullRequest(PullNumber, labels: ["prod", "prod-lock"]), run);

        var (_, body) = Assert.Single(_comments);
        Assert.Equal("""
            [![prod](https://badgen.net/badge/prod/Rollback%20Started/cccc00?labelColor=1d8348&icon=github&scale=1.2)](https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/7065200437/attempts/1 'Open the deploy')
            [Approve deployment](https://github.com/greggbjensen/prdeploy-example-repo/actions/runs/7065200437/attempts/1) to continue

            """.Replace("\r", ""), body);

        // Approval required message is sent to Slack for prod.
        Assert.Contains(_host.Http.Requests, r => r.Request.RequestUri!.ToString() == "https://hooks.slack.com/deploy"
                                                  && r.Body!.Contains("requires"));
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: updates Deploy State And Posts Messages.
    /// </summary>
    [Fact]
    public async Task PostDeployCompletedAsync_UpdatesDeployStateAndPostsMessages()
    {
        var run = GitHubModels.WorkflowRun(7065200437, "[prod] My new feature", conclusion: "success");
        var pullRequest = GitHubModels.PullRequest(PullNumber, labels: ["prod", "prod-lock"], body: "**Release notes**");
        _host.SetupChecks(pullRequest.Head.Ref, ("Build helm chart", "completed", "success", 10014663490));
        _host.SetupBuildRun(10014663490, ".github/workflows/myorg-app-main-build.yml");

        await _service.PostDeployCompletedAsync(pullRequest, run);
        await _service.PostDeployCompletedAsync(pullRequest, run);

        Assert.Contains("Deploy%20Complete", _comments[0].Body);
        var state = await _host.Get<DeployStateService>().GetAsync("prod");
        Assert.Equal(PullNumber, state.PullNumber);
        Assert.Equal("2024.2.26-r8055221191", Assert.Single(state.Services!).Version);

        // The release message is only posted once for the same pull request.
        var releases = _host.Http.Requests.Where(r => r.Request.RequestUri!.ToString() == "https://hooks.slack.com/release").ToList();
        var release = Assert.Single(releases);
        Assert.Contains("*Release notes*", release.Body);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: ignores Cancelled Runs.
    /// </summary>
    [Fact]
    public async Task PostDeployCompletedAsync_IgnoresCancelledRuns()
    {
        var run = GitHubModels.WorkflowRun(7065200437, "[dev] My new feature", conclusion: "cancelled");

        await _service.PostDeployCompletedAsync(GitHubModels.PullRequest(PullNumber, labels: ["dev"]), run);

        Assert.Empty(_comments);
    }

    /// <summary>
    /// Stores or updates the Setup Run.
    /// </summary>
    /// <param name="run">Value supplied for run.</param>
    private void SetupRun(Octokit.WorkflowRun run)
    {
        _host.MockOf(_host.GitHub.Object.Actions.Workflows.Runs)
            .Setup(c => c.Get(WebhookTestHost.Owner, WebhookTestHost.Repo, run.Id))
            .ReturnsAsync(run);
    }

    /// <summary>
    /// Releases resources owned by this instance.
    /// </summary>
    public void Dispose() => _host.Dispose();
}

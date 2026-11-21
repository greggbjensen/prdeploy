using System.Net;
using Moq;
using Octokit;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Services;
using PrDeploy.Api.Webhooks.Status;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>Contains tests that verify prdeploy deploys are recorded as GitHub Deployments.</summary>
public class GitHubDeploymentServiceTest : IDisposable
{
    private const string Owner = WebhookTestHost.Owner;
    private const string Repo = WebhookTestHost.Repo;

    private readonly WebhookTestHost _host = new();
    private readonly GitHubDeploymentService _service;
    private readonly List<NewDeployment> _deployments = [];
    private readonly List<(long DeploymentId, NewDeploymentStatus Status)> _statuses = [];

    /// <summary>Initializes the test fixture and captures created deployments and statuses.</summary>
    public GitHubDeploymentServiceTest()
    {
        _service = _host.Get<GitHubDeploymentService>();
        var deployments = _host.MockOf(_host.GitHub.Object.Repository.Deployment);
        deployments.Setup(c => c.Create(Owner, Repo, It.IsAny<NewDeployment>()))
            .Callback<string, string, NewDeployment>((_, _, d) => _deployments.Add(d))
            .ReturnsAsync(GitHubModels.FromJson<Deployment>("""{ "id": 321 }"""));
        _host.MockOf(_host.GitHub.Object.Repository.Deployment.Status)
            .Setup(c => c.Create(Owner, Repo, It.IsAny<long>(), It.IsAny<NewDeploymentStatus>()))
            .Callback<string, string, long, NewDeploymentStatus>((_, _, id, s) => _statuses.Add((id, s)))
            .ReturnsAsync((DeploymentStatus)null!);
    }

    /// <summary>Verifies a dispatched deploy creates a queued deployment for the pull request's head commit that GitHub will accept.</summary>
    [Fact]
    public async Task CreateQueuedAsync_CreatesQueuedDeploymentForHeadCommit()
    {
        var id = await _service.CreateQueuedAsync(GitHubModels.PullRequest(8), "prod", "[prod] feature-branch: Fix",
            "combined-deploy.yaml", rollback: false);

        Assert.Equal(321, id);
        var deployment = Assert.Single(_deployments);
        Assert.Equal(GitHubModels.HeadSha("feature-branch"), deployment.Ref);
        Assert.Equal("prod", deployment.Environment);
        Assert.Equal(GitHubDeploymentService.DeployTask, deployment.Task);
        Assert.False(deployment.AutoMerge);
        Assert.Empty(deployment.RequiredContexts);
        Assert.True(deployment.ProductionEnvironment);
        Assert.Equal("8", deployment.Payload[GitHubDeploymentService.PullNumberKey]);

        var (deploymentId, status) = Assert.Single(_statuses);
        Assert.Equal(321, deploymentId);
        Assert.Equal(DeploymentState.Queued, status.State);
        Assert.Equal($"{WebhookTestHost.RepositoryUrl}/actions/workflows/combined-deploy.yaml", status.LogUrl);
    }

    /// <summary>Verifies a rollback deployment says so and a non-release environment is not marked as production.</summary>
    [Fact]
    public async Task CreateQueuedAsync_DescribesRollback()
    {
        await _service.CreateQueuedAsync(GitHubModels.PullRequest(8), "dev", "[dev ROLLBACK #8] feature-branch: Fix",
            "combined-deploy.yaml", rollback: true);

        var deployment = Assert.Single(_deployments);
        Assert.StartsWith("Rollback: ", deployment.Description);
        Assert.False(deployment.ProductionEnvironment);
        Assert.Equal("true", deployment.Payload["rollback"]);
    }

    /// <summary>Verifies a GitHub error, such as a missing Deployments permission, is logged instead of stopping the deploy.</summary>
    [Fact]
    public async Task CreateQueuedAsync_ReturnsNullWhenGitHubRefuses()
    {
        _host.MockOf(_host.GitHub.Object.Repository.Deployment)
            .Setup(c => c.Create(Owner, Repo, It.IsAny<NewDeployment>()))
            .ThrowsAsync(new ForbiddenException(new Mock<IResponse>().Object));

        var id = await _service.CreateQueuedAsync(GitHubModels.PullRequest(8), "dev", "[dev] b: c",
            "combined-deploy.yaml", rollback: false);

        Assert.Null(id);
        Assert.Empty(_statuses);
    }

    /// <summary>Verifies a successful run marks the pull request's newest prdeploy deployment in the environment successful, with the environment URL and run log.</summary>
    [Fact]
    public async Task UpdateFromRunAsync_MarksMatchingDeploymentSuccessful()
    {
        SetupDeployments(
            Deployment(100, "dev2", 8, "2026-11-20T10:00:00Z"),
            Deployment(101, "dev2", 8, "2026-11-21T10:00:00Z"),
            Deployment(102, "dev2", 9, "2026-11-21T11:00:00Z"),
            Deployment(103, "dev2", 8, "2026-11-21T12:00:00Z", task: "deploy"));

        await _service.UpdateFromRunAsync(8, "dev2", DeployWorkflowState.Successful,
            GitHubModels.WorkflowRun(5001, "[dev2] feature-branch: Fix"));

        var (deploymentId, status) = Assert.Single(_statuses);
        Assert.Equal(101, deploymentId);
        Assert.Equal(DeploymentState.Success, status.State);
        Assert.Equal("https://dev2.myorg.com", status.EnvironmentUrl);
        Assert.Equal($"{WebhookTestHost.RepositoryUrl}/actions/runs/5001", status.LogUrl);
        Assert.True(status.AutoInactive);
    }

    /// <summary>Verifies running and failed runs map to the matching deployment states without an environment URL.</summary>
    /// <param name="state">Deploy workflow state.</param>
    /// <param name="expected">Expected deployment state.</param>
    [Theory]
    [InlineData(DeployWorkflowState.InProgress, DeploymentState.InProgress)]
    [InlineData(DeployWorkflowState.Failure, DeploymentState.Failure)]
    public async Task UpdateFromRunAsync_MapsRunState(DeployWorkflowState state, DeploymentState expected)
    {
        SetupDeployments(Deployment(100, "dev", 8, "2026-11-20T10:00:00Z"));

        await _service.UpdateFromRunAsync(8, "dev", state, GitHubModels.WorkflowRun(5001, "[dev] b: c"));

        var (_, status) = Assert.Single(_statuses);
        Assert.Equal(expected, status.State);
        Assert.Null(status.EnvironmentUrl);
    }

    /// <summary>Verifies nothing is posted when prdeploy created no deployment for the pull request, such as before the integration was enabled.</summary>
    [Fact]
    public async Task UpdateFromRunAsync_DoesNothingWithoutDeployment()
    {
        SetupDeployments(Deployment(100, "stage", 8, "2026-11-20T10:00:00Z"));

        await _service.UpdateFromRunAsync(8, "dev", DeployWorkflowState.Successful,
            GitHubModels.WorkflowRun(5001, "[dev] b: c"));

        Assert.Empty(_statuses);
    }

    /// <summary>Verifies only absolute http or https environment URLs are used, preferring the labelled URLs.</summary>
    [Fact]
    public void GetEnvironmentUrl_UsesFirstValidUrl()
    {
        Assert.Equal("https://web.myorg.com", GitHubDeploymentService.GetEnvironmentUrl(new EnvironmentSettings
        {
            Url = "https://legacy.myorg.com",
            Urls = [new EnvironmentUrl { Label = "Web", Url = "https://web.myorg.com" }]
        }));
        Assert.Equal("https://legacy.myorg.com",
            GitHubDeploymentService.GetEnvironmentUrl(new EnvironmentSettings { Url = "https://legacy.myorg.com" }));
        Assert.Null(GitHubDeploymentService.GetEnvironmentUrl(new EnvironmentSettings { Url = "javascript:alert(1)" }));
        Assert.Null(GitHubDeploymentService.GetEnvironmentUrl(null));
    }

    /// <summary>Returns the given deployments from the repository's deployment list.</summary>
    /// <param name="deployments">Deployments to return, newest or oldest first.</param>
    private void SetupDeployments(params Deployment[] deployments)
    {
        _host.MockOf(_host.GitHub.Object.Repository.Deployment)
            .Setup(c => c.GetAll(Owner, Repo, It.IsAny<ApiOptions>()))
            .ReturnsAsync(deployments);
    }

    /// <summary>Builds a deployment as GitHub returns it.</summary>
    /// <param name="id">Deployment ID.</param>
    /// <param name="environment">Deployment environment.</param>
    /// <param name="pullNumber">Pull request number in the payload.</param>
    /// <param name="createdAt">Creation time in ISO 8601.</param>
    /// <param name="task">Deployment task; defaults to the prdeploy task.</param>
    /// <returns>The deployment.</returns>
    private static Deployment Deployment(long id, string environment, int pullNumber, string createdAt,
        string task = GitHubDeploymentService.DeployTask) =>
        GitHubModels.FromJson<Deployment>($$"""
            {
              "id": {{id}},
              "environment": "{{environment}}",
              "task": "{{task}}",
              "created_at": "{{createdAt}}",
              "payload": { "pull_number": "{{pullNumber}}" }
            }
            """);

    /// <summary>Releases resources owned by this instance.</summary>
    public void Dispose() => _host.Dispose();
}

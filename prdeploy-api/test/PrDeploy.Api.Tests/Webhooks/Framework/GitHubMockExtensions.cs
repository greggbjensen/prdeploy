using Moq;
using Octokit;

namespace PrDeploy.Api.Tests.Webhooks.Framework;

public static class GitHubMockExtensions
{
    private const string Owner = WebhookTestHost.Owner;
    private const string Repo = WebhookTestHost.Repo;

    /// <summary>
    /// Mocks a build workflow run with a build-details artifact containing version 2024.2.26-r8055221191.
    /// </summary>
    public static void SetupBuildRun(this WebhookTestHost host, long runId, string path)
    {
        var github = host.GitHub.Object;
        host.MockOf(github.Actions.Workflows.Runs)
            .Setup(c => c.Get(Owner, Repo, runId))
            .ReturnsAsync(GitHubModels.WorkflowRun(runId, "Build", path));

        host.MockOf(github.Actions.Artifacts)
            .Setup(c => c.ListWorkflowArtifacts(Owner, Repo, runId, It.IsAny<ListArtifactsRequest>()))
            .ReturnsAsync(GitHubModels.FromJson<ListArtifactsResponse>(
                """{ "total_count": 1, "artifacts": [{ "id": 654321, "name": "build-details" }] }"""));

        host.MockOf(github.Actions.Artifacts)
            .Setup(c => c.DownloadArtifact(Owner, Repo, 654321, "zip"))
            .ReturnsAsync(() => File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Webhooks", "Mocks",
                "build-details.zip")));
    }

    public static void SetupChecks(this WebhookTestHost host, string reference,
        params (string Name, string Status, string? Conclusion, long RunId)[] checks)
    {
        host.MockOf(host.GitHub.Object.Check.Run)
            .Setup(c => c.GetAllForReference(Owner, Repo, reference, It.IsAny<CheckRunRequest>(), It.IsAny<ApiOptions>()))
            .ReturnsAsync(GitHubModels.CheckRuns(checks));
    }

    public static void SetupPullRequest(this WebhookTestHost host, PullRequest pullRequest)
    {
        host.MockOf(host.GitHub.Object.PullRequest)
            .Setup(c => c.Get(Owner, Repo, pullRequest.Number))
            .ReturnsAsync(pullRequest);
    }

    public static void SetupDeployedPullRequests(this WebhookTestHost host, params PullRequest[] pullRequests)
    {
        host.MockOf(host.GitHub.Object.PullRequest)
            .Setup(c => c.GetAllForRepository(Owner, Repo, It.IsAny<PullRequestRequest>(), It.IsAny<ApiOptions>()))
            .ReturnsAsync(pullRequests);
    }

    public static void SetupUser(this WebhookTestHost host, string login, string? name = null)
    {
        host.MockOf(host.GitHub.Object.User)
            .Setup(c => c.Get(login))
            .ReturnsAsync(GitHubModels.FromJson<User>(
                System.Text.Json.JsonSerializer.Serialize(new { login, name, email = "" })));
    }

    /// <summary>
    /// Captures the labels set on each pull request.
    /// </summary>
    public static Dictionary<long, string[]> CaptureLabels(this WebhookTestHost host)
    {
        var labels = new Dictionary<long, string[]>();
        host.MockOf(host.GitHub.Object.Issue.Labels)
            .Setup(c => c.ReplaceAllForIssue(Owner, Repo, It.IsAny<long>(), It.IsAny<string[]>()))
            .Callback<string, string, long, string[]>((_, _, number, l) => labels[number] = l)
            .ReturnsAsync([]);
        return labels;
    }

    /// <summary>
    /// Captures the comments created on each pull request.
    /// </summary>
    public static List<(long Number, string Body)> CaptureComments(this WebhookTestHost host)
    {
        var comments = new List<(long, string)>();
        host.MockOf(host.GitHub.Object.Issue.Comment)
            .Setup(c => c.Create(Owner, Repo, It.IsAny<long>(), It.IsAny<string>()))
            .Callback<string, string, long, string>((_, _, number, body) => comments.Add((number, body)))
            .ReturnsAsync((IssueComment)null!);
        return comments;
    }

    public static List<(string Workflow, CreateWorkflowDispatch Dispatch)> CaptureDispatches(this WebhookTestHost host)
    {
        var dispatches = new List<(string, CreateWorkflowDispatch)>();
        host.MockOf(host.GitHub.Object.Actions.Workflows)
            .Setup(c => c.CreateDispatch(Owner, Repo, It.IsAny<string>(), It.IsAny<CreateWorkflowDispatch>()))
            .Callback<string, string, string, CreateWorkflowDispatch>((_, _, workflow, d) => dispatches.Add((workflow, d)))
            .Returns(Task.CompletedTask);
        return dispatches;
    }
}

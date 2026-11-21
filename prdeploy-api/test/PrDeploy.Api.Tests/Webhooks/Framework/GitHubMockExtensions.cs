using Moq;
using Octokit;

namespace PrDeploy.Api.Tests.Webhooks.Framework;

/// <summary>
/// Represents git hub mock extensions data used by the application.
/// </summary>
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

    /// <summary>
    /// Stores or updates the setup checks.
    /// </summary>
    /// <param name="host">Object containing the host data used by the operation.</param>
    /// <param name="reference">Text used as the reference.</param>
    /// <param name="checks">Object containing the checks data used by the operation.</param>
    public static void SetupChecks(this WebhookTestHost host, string reference,
        params (string Name, string Status, string? Conclusion, long RunId)[] checks)
    {
        host.MockOf(host.GitHub.Object.Check.Run)
            .Setup(c => c.GetAllForReference(Owner, Repo, reference, It.IsAny<CheckRunRequest>(), It.IsAny<ApiOptions>()))
            .ReturnsAsync(GitHubModels.CheckRuns(checks));
    }

    /// <summary>
    /// Sets up the check runs returned for a ref, where each run may carry output text such as the build marker.
    /// </summary>
    /// <param name="host">Test host whose GitHub client is configured.</param>
    /// <param name="reference">Git ref the check runs are listed for.</param>
    /// <param name="checks">Check runs to return; a null output text omits the run's output.</param>
    public static void SetupChecks(this WebhookTestHost host, string reference,
        params (string Name, string Status, string? Conclusion, long RunId, string? OutputText)[] checks)
    {
        host.MockOf(host.GitHub.Object.Check.Run)
            .Setup(c => c.GetAllForReference(Owner, Repo, reference, It.IsAny<CheckRunRequest>(), It.IsAny<ApiOptions>()))
            .ReturnsAsync(GitHubModels.CheckRuns(checks));
    }

    /// <summary>
    /// Stores or updates the setup pull request.
    /// </summary>
    /// <param name="host">Object containing the host data used by the operation.</param>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    public static void SetupPullRequest(this WebhookTestHost host, PullRequest pullRequest)
    {
        host.MockOf(host.GitHub.Object.PullRequest)
            .Setup(c => c.Get(Owner, Repo, pullRequest.Number))
            .ReturnsAsync(pullRequest);
    }

    /// <summary>
    /// Stores or updates the setup deployed pull requests.
    /// </summary>
    /// <param name="host">Object containing the host data used by the operation.</param>
    /// <param name="pullRequests">Object containing the pull requests data used by the operation.</param>
    public static void SetupDeployedPullRequests(this WebhookTestHost host, params PullRequest[] pullRequests)
    {
        host.MockOf(host.GitHub.Object.PullRequest)
            .Setup(c => c.GetAllForRepository(Owner, Repo, It.IsAny<PullRequestRequest>(), It.IsAny<ApiOptions>()))
            .ReturnsAsync(pullRequests);
    }

    /// <summary>
    /// Stores or updates the setup user.
    /// </summary>
    /// <param name="host">Object containing the host data used by the operation.</param>
    /// <param name="login">Text used as the login.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    public static void SetupUser(this WebhookTestHost host, string login, string? name = null)
    {
        host.MockOf(host.GitHub.Object.User)
            .Setup(c => c.Get(login))
            .ReturnsAsync(GitHubModels.FromJson<User>(
                System.Text.Json.JsonSerializer.Serialize(new { login, name, email = "" })));
    }

    /// <summary>
    /// Sets the repository permission GitHub reports for a user, which gates comment commands.
    /// </summary>
    /// <param name="host">Test host whose GitHub client is configured.</param>
    /// <param name="login">User whose permission is configured.</param>
    /// <param name="permission">Legacy permission: admin, write, read, or none.</param>
    /// <param name="roleName">Role name, such as maintain or triage; defaults to the permission.</param>
    public static void SetupCollaboratorPermission(this WebhookTestHost host, string login, string permission,
        string? roleName = null)
    {
        host.MockOf(host.GitHub.Object.Repository.Collaborator)
            .Setup(c => c.ReviewPermission(Owner, Repo, login))
            .ReturnsAsync(GitHubModels.FromJson<CollaboratorPermissionResponse>(
                System.Text.Json.JsonSerializer.Serialize(new { permission, role_name = roleName ?? permission })));
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

    /// <summary>
    /// Performs the capture dispatches operation and returns its result.
    /// </summary>
    /// <param name="host">Object containing the host data used by the operation.</param>
    /// <returns>The collection of items produced by the operation.</returns>
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

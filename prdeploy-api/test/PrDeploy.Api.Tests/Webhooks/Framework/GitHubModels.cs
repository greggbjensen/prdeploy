using Octokit;
using Octokit.Internal;

namespace PrDeploy.Api.Tests.Webhooks.Framework;

/// <summary>
/// Builds Octokit models from GitHub API JSON, since they have no public setters.
/// </summary>
public static class GitHubModels
{
    private static readonly SimpleJsonSerializer Serializer = new();

    /// <summary>
    /// Performs the from Json operation and returns its result.
    /// </summary>
    /// <param name="json">Value supplied for json.</param>
    /// <typeparam name="T">Type used by this operation.</typeparam>
    /// <returns>The value produced by this operation.</returns>
    public static T FromJson<T>(string json) => Serializer.Deserialize<T>(json);

    /// <summary>
    /// Performs the pull Request operation and returns its result.
    /// </summary>
    /// <param name="number">Value supplied for number.</param>
    /// <param name="branch">Branch associated with the operation.</param>
    /// <param name="login">Value supplied for login.</param>
    /// <param name="labels">Value supplied for labels.</param>
    /// <param name="body">Value supplied for body.</param>
    /// <param name="merged">Value supplied for merged.</param>
    /// <param name="title">Value supplied for title.</param>
    /// <returns>The value produced by this operation.</returns>
    public static PullRequest PullRequest(int number, string branch = "feature-branch", string login = "greggbjensen",
        string[]? labels = null, string body = "", bool merged = false, string title = "My new feature")
    {
        var labelJson = string.Join(",", (labels ?? []).Select(l => $$"""{ "name": "{{l}}" }"""));
        return FromJson<PullRequest>($$"""
            {
              "number": {{number}},
              "title": "{{title}}",
              "body": {{System.Text.Json.JsonSerializer.Serialize(body)}},
              "html_url": "https://github.com/{{WebhookTestHost.Owner}}/{{WebhookTestHost.Repo}}/pull/{{number}}",
              "merged": {{merged.ToString().ToLowerInvariant()}},
              "merged_at": {{(merged ? "\"2026-11-15T12:00:00Z\"" : "null")}},
              "mergeable": true,
              "mergeable_state": "clean",
              "user": { "login": "{{login}}" },
              "head": { "ref": "{{branch}}" },
              "base": { "ref": "main" },
              "labels": [{{labelJson}}]
            }
            """);
    }

    /// <summary>
    /// Performs the workflow Run operation and returns its result.
    /// </summary>
    /// <param name="id">Value supplied for id.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="path">Path used to locate the resource.</param>
    /// <param name="conclusion">Value supplied for conclusion.</param>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <param name="runAttempt">Value supplied for run Attempt.</param>
    /// <returns>The value produced by this operation.</returns>
    public static WorkflowRun WorkflowRun(long id, string name, string path = ".github/workflows/deploy.yml",
        string? conclusion = null, int? pullNumber = null, int runAttempt = 1)
    {
        var pulls = pullNumber.HasValue ? $$"""[{ "number": {{pullNumber}} }]""" : "[]";
        var conclusionJson = conclusion != null ? $"\"{conclusion}\"" : "null";
        return FromJson<WorkflowRun>($$"""
            {
              "id": {{id}},
              "name": "{{name}}",
              "display_title": "{{name}}",
              "path": "{{path}}",
              "run_attempt": {{runAttempt}},
              "status": "completed",
              "conclusion": {{conclusionJson}},
              "html_url": "https://github.com/{{WebhookTestHost.Owner}}/{{WebhookTestHost.Repo}}/actions/runs/{{id}}",
              "pull_requests": {{pulls}}
            }
            """);
    }

    /// <summary>
    /// Checks whether the runs condition is satisfied.
    /// </summary>
    /// <param name="checks">Value supplied for checks.</param>
    /// <returns>The value produced by this operation.</returns>
    public static CheckRunsResponse CheckRuns(params (string Name, string Status, string? Conclusion, long RunId)[] checks)
    {
        var json = string.Join(",", checks.Select(c => $$"""
            {
              "name": "{{c.Name}}",
              "status": "{{c.Status}}",
              "conclusion": {{(c.Conclusion != null ? $"\"{c.Conclusion}\"" : "null")}},
              "details_url": "https://github.com/{{WebhookTestHost.Owner}}/{{WebhookTestHost.Repo}}/actions/runs/{{c.RunId}}/job/27684771116",
              "html_url": "https://github.com/{{WebhookTestHost.Owner}}/{{WebhookTestHost.Repo}}/runs/{{c.RunId}}"
            }
            """));
        return FromJson<CheckRunsResponse>($$"""{ "total_count": {{checks.Length}}, "check_runs": [{{json}}] }""");
    }
}

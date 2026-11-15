using Octokit;
using Octokit.Internal;

namespace PrDeploy.Api.Tests.Webhooks.Framework;

/// <summary>
/// Builds Octokit models from GitHub API JSON, since they have no public setters.
/// </summary>
public static class GitHubModels
{
    private static readonly SimpleJsonSerializer Serializer = new();

    public static T FromJson<T>(string json) => Serializer.Deserialize<T>(json);

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

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
    /// Performs the from JSON operation and returns its result.
    /// </summary>
    /// <param name="json">Text used as the JSON.</param>
    /// <typeparam name="T">Type of value consumed or produced by this operation.</typeparam>
    /// <returns>The t produced by the operation.</returns>
    public static T FromJson<T>(string json) => Serializer.Deserialize<T>(json);

    /// <summary>
    /// Performs the pull request operation and returns its result.
    /// </summary>
    /// <param name="number">Numeric value used for the number.</param>
    /// <param name="branch">Branch associated with the operation.</param>
    /// <param name="login">Text used as the login.</param>
    /// <param name="labels">Object containing the labels data used by the operation.</param>
    /// <param name="body">Text used as the body.</param>
    /// <param name="merged">Whether the merged option is enabled.</param>
    /// <param name="title">Text used as the title.</param>
    /// <returns>The pull request produced by the operation.</returns>
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
    /// Performs the workflow run operation and returns its result.
    /// </summary>
    /// <param name="id">Numeric value used for the id.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <param name="path">Path used to locate the resource.</param>
    /// <param name="conclusion">Text used as the conclusion.</param>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <param name="runAttempt">Numeric value used for the run attempt.</param>
    /// <returns>The workflow run produced by the operation.</returns>
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
    /// <param name="checks">Object containing the checks data used by the operation.</param>
    /// <returns>The check runs response produced by the operation.</returns>
    public static CheckRunsResponse CheckRuns(params (string Name, string Status, string? Conclusion, long RunId)[] checks)
    {
        return CheckRuns(checks.Select(c => (c.Name, c.Status, c.Conclusion, c.RunId, (string?)null)).ToArray());
    }

    /// <summary>
    /// Builds a check runs response whose runs may carry output text, such as the build marker written by the
    /// build-details action.
    /// </summary>
    /// <param name="checks">Check runs to include; a null output text omits the <c>output</c> object entirely.</param>
    /// <returns>The check runs response produced by the operation.</returns>
    public static CheckRunsResponse CheckRuns(
        params (string Name, string Status, string? Conclusion, long RunId, string? OutputText)[] checks)
    {
        var json = string.Join(",", checks.Select(c =>
        {
            var output = c.OutputText != null
                ? $$""", "output": { "text": {{System.Text.Json.JsonSerializer.Serialize(c.OutputText)}} }"""
                : string.Empty;
            return $$"""
                {
                  "name": "{{c.Name}}",
                  "status": "{{c.Status}}",
                  "conclusion": {{(c.Conclusion != null ? $"\"{c.Conclusion}\"" : "null")}},
                  "details_url": "https://github.com/{{WebhookTestHost.Owner}}/{{WebhookTestHost.Repo}}/actions/runs/{{c.RunId}}/job/27684771116",
                  "html_url": "https://github.com/{{WebhookTestHost.Owner}}/{{WebhookTestHost.Repo}}/runs/{{c.RunId}}"{{output}}
                }
                """;
        }));
        return FromJson<CheckRunsResponse>($$"""{ "total_count": {{checks.Length}}, "check_runs": [{{json}}] }""");
    }
}

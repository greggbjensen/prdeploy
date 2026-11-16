using System.Collections.Concurrent;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HandlebarsDotNet;
using Octokit;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>
/// Renders the Handlebars templates for GitHub comments and Slack messages.
/// </summary>
/// <remarks>
/// Template contexts are converted to camelCase dictionaries so templates keep handlebars.js style names.
/// GitHub objects should be passed through <see cref="Pull"/>, <see cref="Run"/> or <see cref="Check"/>,
/// which expose the snake_case names from the GitHub API used by the templates.
/// </remarks>
public class TemplateService
{
    public const string DeployStarted = "deploy-started.md";
    public const string DeployCompletedJson = "deploy-completed.json";
    public const string DeployCompleted = "deploy-completed.md";
    public const string DeployNothingFoundJson = "deploy-nothing-found.json";
    public const string DeployNothingFound = "deploy-nothing-found.md";
    public const string DeployFailed = "deploy-failed.md";
    public const string DeployReleasedJson = "deploy-released.json";
    public const string EnvironmentAvailableJson = "environment-available.json";
    public const string EnvironmentApprovalRequiredJson = "environment-approval-required.json";
    public const string PullRequestChecksIncomplete = "pull-request-checks-incomplete.md";
    public const string PullRequestChecksRedeploy = "pull-request-checks-redeploy.md";
    public const string PullRequestEnqueued = "pull-request-enqueued.md";
    public const string PullRequestIssues = "pull-request-issues.md";
    public const string PullRequestMergeConflictsJson = "pull-request-merge-conflicts.json";
    public const string PullRequestMergeConflicts = "pull-request-merge-conflicts.md";
    public const string PullRequestUpdated = "pull-request-updated.md";
    public const string RollbackCompleted = "rollback-completed.md";
    public const string RollbackStarted = "rollback-started.md";
    public const string ServicesNotFound = "services-not-found.md";

    private static readonly IHandlebars Handlebars = CreateHandlebars();
    private static readonly ConcurrentDictionary<string, HandlebarsTemplate<object, object>> Templates = new();

    private static readonly JsonSerializerOptions ContextSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null
    };

    // Matches JSON.stringify, which does not escape HTML or non-ASCII characters.
    private static readonly JsonSerializerOptions JsonEncodeOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Performs the render operation and returns its result.
    /// </summary>
    /// <param name="templateName">Value supplied for template Name.</param>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public string Render(string templateName, object? context = null)
    {
        var template = Templates.GetOrAdd(templateName, name => Handlebars.Compile(LoadTemplate(name)));
        return template(ToTemplateContext(context));
    }

    /// <summary>
    /// Performs the render Queue Table operation and returns its result.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="normalizedEnvironment">Deployment environment involved in the operation.</param>
    /// <param name="queuePullNumbers">Value supplied for queue Pull Numbers.</param>
    /// <param name="repositoryUrl">Value supplied for repository Url.</param>
    /// <param name="prdeployPortalUrl">Value supplied for prdeploy Portal Url.</param>
    /// <returns>The value produced by this operation.</returns>
    public string RenderQueueTable(string owner, string repo, string normalizedEnvironment,
        IEnumerable<int> queuePullNumbers, string repositoryUrl, string? prdeployPortalUrl)
    {
        // This is dynamic horizontally, so cannot be handled by normal template engine.
        var p = 0;
        var positions = new StringBuilder("| Position |");
        var columns = new StringBuilder("|----------|");
        var pulls = new StringBuilder(
            $"| [{normalizedEnvironment} queue]({prdeployPortalUrl}/{owner}/{repo}/deployments?environment={normalizedEnvironment}) |");
        foreach (var pr in queuePullNumbers)
        {
            p++;
            positions.Append($" {p}       |");
            columns.Append("---------|");
            pulls.Append($" [{pr}]({repositoryUrl}/pull/{pr}) |");
        }

        return $"{positions}\n{columns}\n{pulls}";
    }

    /// <summary>
    /// Performs the pull operation and returns its result.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public static object? Pull(PullRequest? pullRequest) => pullRequest == null
        ? null
        : new
        {
            number = pullRequest.Number,
            title = pullRequest.Title,
            html_url = pullRequest.HtmlUrl,
            body = pullRequest.Body
        };

    /// <summary>
    /// Performs the run operation and returns its result.
    /// </summary>
    /// <param name="run">Value supplied for run.</param>
    /// <returns>The value produced by this operation.</returns>
    public static object? Run(WorkflowRun? run) => run == null
        ? null
        : new
        {
            id = run.Id,
            name = run.Name,
            run_attempt = run.RunAttempt,
            html_url = run.HtmlUrl,
            conclusion = run.Conclusion?.StringValue
        };

    /// <summary>
    /// Checks whether the condition condition is satisfied.
    /// </summary>
    /// <param name="check">Value supplied for check.</param>
    /// <returns>The value produced by this operation.</returns>
    public static object Check(CheckRun check) => new
    {
        name = check.Name,
        html_url = check.HtmlUrl,
        status = check.Status.StringValue,
        conclusion = check.Conclusion?.StringValue
    };

    /// <summary>
    /// Converts the supplied value to the Template Context.
    /// </summary>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <returns>The value produced by this operation.</returns>
    internal static object ToTemplateContext(object? context)
    {
        if (context == null)
        {
            return new Dictionary<string, object?>();
        }

        var element = JsonSerializer.SerializeToElement(context, ContextSerializerOptions);
        return ToTemplateValue(element) ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Converts the supplied value to the Template Value.
    /// </summary>
    /// <param name="element">Value supplied for element.</param>
    /// <returns>The value produced by this operation.</returns>
    private static object? ToTemplateValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(p => p.Name, p => ToTemplateValue(p.Value)),
        JsonValueKind.Array => element.EnumerateArray().Select(ToTemplateValue).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    /// <summary>
    /// Performs the load Template operation and returns its result.
    /// </summary>
    /// <param name="templateName">Value supplied for template Name.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string LoadTemplate(string templateName)
    {
        var assembly = typeof(TemplateService).Assembly;
        using var stream = assembly.GetManifestResourceStream($"Templates.{templateName}")
                           ?? throw new ArgumentException($"Template {templateName} was not found.",
                               nameof(templateName));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r", string.Empty);
    }

    /// <summary>
    /// Creates the Handlebars from the supplied inputs.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private static IHandlebars CreateHandlebars()
    {
        var handlebars = HandlebarsDotNet.Handlebars.Create(new HandlebarsConfiguration
        {
            TextEncoder = new HandlebarsJsTextEncoder()
        });

        handlebars.RegisterHelper("jsonEncode", (Context _, Arguments arguments) =>
            JsonSerializer.Serialize(GetString(arguments), JsonEncodeOptions));

        handlebars.RegisterHelper("jsonEncodeUnquoted", (Context _, Arguments arguments) =>
        {
            var json = JsonSerializer.Serialize(GetString(arguments) ?? string.Empty, JsonEncodeOptions);
            return json[1..^1];
        });

        handlebars.RegisterHelper("color", (Context _, Arguments arguments) =>
            GetString(arguments)?.TrimStart('#') ?? string.Empty);

        return handlebars;
    }

    /// <summary>
    /// Retrieves the String.
    /// </summary>
    /// <param name="arguments">Value supplied for arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string? GetString(Arguments arguments) =>
        arguments.Length > 0 && arguments[0] is not UndefinedBindingResult ? arguments[0]?.ToString() : null;

    /// <summary>
    /// Escapes the same characters as handlebars.js, leaving non-ASCII characters such as names as is.
    /// </summary>
    private sealed class HandlebarsJsTextEncoder : ITextEncoder
    {
        /// <summary>
        /// Performs the encode operation.
        /// </summary>
        /// <param name="text">Value supplied for text.</param>
        /// <param name="target">Target environment or value for the operation.</param>
        public void Encode(StringBuilder text, TextWriter target)
        {
            for (var i = 0; i < text.Length; i++)
            {
                Write(text[i], target);
            }
        }

        /// <summary>
        /// Performs the encode operation.
        /// </summary>
        /// <param name="text">Value supplied for text.</param>
        /// <param name="target">Target environment or value for the operation.</param>
        public void Encode(string text, TextWriter target)
        {
            foreach (var c in text)
            {
                Write(c, target);
            }
        }

        /// <summary>
        /// Performs the encode operation.
        /// </summary>
        /// <param name="text">Value supplied for text.</param>
        /// <param name="target">Target environment or value for the operation.</param>
        /// <typeparam name="T">Type used by this operation.</typeparam>
        public void Encode<T>(T text, TextWriter target) where T : IEnumerator<char>
        {
            while (text.MoveNext())
            {
                Write(text.Current, target);
            }
        }

        /// <summary>
        /// Performs the write operation.
        /// </summary>
        /// <param name="c">Value supplied for c.</param>
        /// <param name="target">Target environment or value for the operation.</param>
        private static void Write(char c, TextWriter target)
        {
            switch (c)
            {
                case '&': target.Write("&amp;"); break;
                case '<': target.Write("&lt;"); break;
                case '>': target.Write("&gt;"); break;
                case '"': target.Write("&quot;"); break;
                case '\'': target.Write("&#x27;"); break;
                case '`': target.Write("&#x60;"); break;
                case '=': target.Write("&#x3D;"); break;
                default: target.Write(c); break;
            }
        }
    }
}

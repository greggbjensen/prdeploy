using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>
/// Dispatches the configured automation test workflow after a successful deploy.
/// </summary>
public partial class AutomationTestService
{
    private readonly WebhookContext _context;
    private readonly PullRequestService _pullRequestService;
    private readonly EnvironmentUtil _environmentUtil;
    private readonly ILogger<AutomationTestService> _logger;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <param name="pullRequestService">Object containing the pull request service data used by the operation.</param>
    /// <param name="environmentUtil">Object containing the environment util data used by the operation.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public AutomationTestService(WebhookContext context, PullRequestService pullRequestService,
        EnvironmentUtil environmentUtil, ILogger<AutomationTestService> logger)
    {
        _context = context;
        _pullRequestService = pullRequestService;
        _environmentUtil = environmentUtil;
        _logger = logger;
    }

    /// <summary>
    /// Performs the trigger test run operation.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="run">Object containing the run data used by the operation.</param>
    public async Task TriggerTestRunAsync(PullRequest pullRequest, WorkflowRun run)
    {
        _logger.LogDebug("Evaluating automation test dispatch for pull #{PullNumber}.", pullRequest.Number);
        var environment = _pullRequestService.GetEnvironment(pullRequest, run);
        var automationSettings = _environmentUtil.GetSettings(environment)?.AutomationTest;
        if (automationSettings?.Enabled != true || string.IsNullOrEmpty(automationSettings.Workflow))
        {
            return;
        }

        var context = new Dictionary<string, object?>
        {
            ["environment"] = environment,
            ["pull_number"] = pullRequest.Number,
            ["run_name"] = run.Name
        };
        var inputs = ReplaceAutomationTokens(automationSettings.Inputs ?? [], context);
        _logger.LogInformation("Dispatching automation test workflow: {Workflow}: {Inputs}",
            automationSettings.Workflow, JsonSerializer.Serialize(inputs, new JsonSerializerOptions { WriteIndented = true }));

        await _context.GitHub.Actions.Workflows.CreateDispatch(_context.Owner, _context.Repo,
            automationSettings.Workflow, new CreateWorkflowDispatch(pullRequest.Head.Ref) { Inputs = inputs });
    }

    /// <summary>
    /// Performs the replace automation tokens operation and returns its result.
    /// </summary>
    /// <param name="inputs">Object containing the inputs data used by the operation.</param>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <returns>The dictionary&lt;string, object&gt; produced by the operation.</returns>
    internal Dictionary<string, object> ReplaceAutomationTokens(IDictionary<string, object> inputs,
        IReadOnlyDictionary<string, object?> context)
    {
        return inputs.ToDictionary(i => i.Key, i => (object)TokenRegex().Replace(i.Value?.ToString() ?? string.Empty,
            match =>
            {
                var key = match.Groups[1].Value;
                if (context.TryGetValue(key, out var value) && value != null)
                {
                    return value.ToString()!;
                }

                _logger.LogWarning("Automation workflow inputs, no context match found for {Key}.", key);
                return match.Value;
            }));
    }

    /// <summary>
    /// Converts the supplied value to the token regex.
    /// </summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"\$\{(.*?)\}")]
    private static partial Regex TokenRegex();
}

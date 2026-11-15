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

    public AutomationTestService(WebhookContext context, PullRequestService pullRequestService,
        EnvironmentUtil environmentUtil, ILogger<AutomationTestService> logger)
    {
        _context = context;
        _pullRequestService = pullRequestService;
        _environmentUtil = environmentUtil;
        _logger = logger;
    }

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

    [GeneratedRegex(@"\$\{(.*?)\}")]
    private static partial Regex TokenRegex();
}

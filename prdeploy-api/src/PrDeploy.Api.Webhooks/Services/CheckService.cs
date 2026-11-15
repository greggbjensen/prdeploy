using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Webhooks.Models;
using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Webhooks.Services;

public partial class CheckService
{
    private const int MaxPageSize = 100;
    private const string BuildDetailsArtifact = "build-details";
    private const string BuildDetailsFile = "build-details.json";
    private static readonly string[] ValidMergeableStates = ["clean", "behind", "blocked"];
    private static readonly JsonSerializerOptions BuildDetailsSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly WebhookContext _context;
    private readonly TemplateService _templateService;
    private readonly SlackService _slackService;
    private readonly PullRequestService _pullRequestService;
    private readonly EnvironmentUtil _environmentUtil;
    private readonly ILogger<CheckService> _logger;

    public CheckService(WebhookContext context, TemplateService templateService, SlackService slackService,
        PullRequestService pullRequestService, EnvironmentUtil environmentUtil, ILogger<CheckService> logger)
    {
        _context = context;
        _templateService = templateService;
        _slackService = slackService;
        _pullRequestService = pullRequestService;
        _environmentUtil = environmentUtil;
        _logger = logger;
    }

    private DeploySettings Settings => _context.Settings;

    public async Task<bool> VerifyDeployReadyAsync(PullRequest pullRequest, string? environment = null)
    {
        var deployEnvironment = _pullRequestService.GetDefaultedEnvironment(pullRequest, environment);
        _logger.LogDebug("Checking if pull {PullNumber} is ready for deploy to {Environment}.",
            pullRequest.Number, deployEnvironment);

        var environmentSettings = _environmentUtil.GetRequiredSettings(deployEnvironment);
        var valid = true;
        if (environmentSettings.RequireBranchUpToDate)
        {
            valid = await VerifyBranchUpToDateAsync(pullRequest, environmentSettings);
        }

        if (valid)
        {
            valid = await VerifyPullChecksCompleteAsync(pullRequest, environmentSettings);
        }

        return valid;
    }

    public async Task<List<Build>> ListBuildsAsync(PullRequest pullRequest, IReadOnlyCollection<long>? overrideBuildRunIds = null)
    {
        if (string.IsNullOrEmpty(Settings.Builds?.WorkflowPattern))
        {
            return [];
        }

        var workflowRegex = new Regex(Settings.Builds.WorkflowPattern, RegexOptions.IgnoreCase);

        if (overrideBuildRunIds is { Count: > 0 })
        {
            var overrideBuilds = await Task.WhenAll(overrideBuildRunIds.Select(r => ExtractBuildAsync(workflowRegex, r)));
            return overrideBuilds.ToList();
        }

        _logger.LogInformation("Listing checks for ref: {Ref}", pullRequest.Head.Ref);
        var result = await GetChecksForRefAsync(pullRequest.Head.Ref);

        var checkRegex = new Regex(Settings.Builds.CheckPattern ?? string.Empty, RegexOptions.IgnoreCase);
        var buildChecks = result.CheckRuns.Where(c => checkRegex.IsMatch(c.Name)).ToList();
        var incompleteChecks = buildChecks
            .Where(c => c.Status.StringValue != "completed")
            .Select(c => $"{c.Name} ({c.Status.StringValue})")
            .ToList();
        if (incompleteChecks.Count > 0)
        {
            _logger.LogWarning("The following checks are incomplete: {Checks}", string.Join(", ", incompleteChecks));
            _logger.LogWarning("Please wait for the checks to pass and try again.");
        }

        var builds = await Task.WhenAll(buildChecks.Select(c => ExtractBuildAsync(workflowRegex, ParseRunId(c.DetailsUrl))));

        // Remove any duplicate builds.
        var uniqueBuilds = builds.DistinctBy(b => b.Name).ToList();
        _logger.LogDebug("Found builds:\n{Builds}", JsonSerializer.Serialize(uniqueBuilds));

        return uniqueBuilds;
    }

    public async Task<List<CheckRun>> ListIncompletePullChecksAsync(PullRequest pullRequest)
    {
        _logger.LogInformation("Listing checks for ref: {Ref}", pullRequest.Head.Ref);
        var result = await GetChecksForRefAsync(pullRequest.Head.Ref);

        var incompleteChecks = result.CheckRuns
            .Where(c => c.Status.StringValue != "completed" || c.Conclusion?.StringValue == "failure")
            .ToList();

        if (incompleteChecks.Count == 0)
        {
            _logger.LogDebug("All checks are complete.");
            return incompleteChecks;
        }

        _logger.LogDebug("Incomplete checks: {Checks}", string.Join(", ", incompleteChecks.Select(c => c.Name)));

        // We need to exclude any workflows that could be triggered by PR Deploy directly.
        var excludedWorkflows = GetExcludedWorkflows(Settings);
        _logger.LogDebug("Filtering out any checks that are from workflows: {Workflows}",
            string.Join(", ", excludedWorkflows));

        var keep = await Task.WhenAll(incompleteChecks.Select(async c =>
        {
            try
            {
                var run = await _context.GitHub.Actions.Workflows.Runs.Get(_context.Owner, _context.Repo,
                    ParseRunId(c.DetailsUrl));
                var checkWorkflow = Path.GetFileName(run.Path).ToLowerInvariant();
                var keepCheck = !excludedWorkflows.Contains(checkWorkflow);
                _logger.LogDebug("Keep {Check} ({Workflow}): {Keep}", c.Name, checkWorkflow, keepCheck);
                return keepCheck;
            }
            catch (Exception)
            {
                _logger.LogWarning("Unable to retrieve workflow for check: {Url}", c.HtmlUrl);
                return true;
            }
        }));

        var retainedChecks = incompleteChecks.Where((_, i) => keep[i]).ToList();
        _logger.LogDebug("Filtered checks: {Checks}", string.Join(", ", retainedChecks.Select(c => c.Name)));

        return retainedChecks;
    }

    /// <summary>
    /// Lists the workflows triggered by PR Deploy itself, which should not retrigger deploys.
    /// </summary>
    public static List<string> GetExcludedWorkflows(DeploySettings settings)
    {
        var excludedWorkflows = (settings.Environments ?? [])
            .Where(e => e.AutomationTest?.Enabled == true && !string.IsNullOrEmpty(e.AutomationTest.Workflow))
            .Select(e => e.AutomationTest!.Workflow!.ToLowerInvariant())
            .Distinct()
            .ToList();
        excludedWorkflows.Add((settings.DeployWorkflow ?? string.Empty).ToLowerInvariant());
        return excludedWorkflows;
    }

    private async Task<CheckRunsResponse> GetChecksForRefAsync(string reference)
    {
        return await _context.GitHub.Check.Run.GetAllForReference(_context.Owner, _context.Repo, reference,
            new CheckRunRequest(), new ApiOptions { PageSize = MaxPageSize, PageCount = 1, StartPage = 1 });
    }

    private static long ParseRunId(string? url)
    {
        var match = RunRegex().Match(url ?? string.Empty);
        return match.Success ? long.Parse(match.Groups[1].Value) : 0;
    }

    private async Task<bool> VerifyPullChecksCompleteAsync(PullRequest pullRequest,
        EnvironmentSettings environmentSettings)
    {
        var incompleteChecks = await ListIncompletePullChecksAsync(pullRequest);

        var valid = incompleteChecks.Count == 0;
        if (!valid)
        {
            _logger.LogInformation("Pull request {PullNumber} checks are not complete.", pullRequest.Number);
            var body = _templateService.Render(TemplateService.PullRequestChecksIncomplete, new
            {
                owner = _context.Owner,
                repo = _context.Repo,
                environment = environmentSettings,
                badge = Settings.Badge,
                prdeployPortalUrl = Settings.PrdeployPortalUrl,
                incompleteChecks = incompleteChecks.Select(TemplateService.Check)
            });

            await _context.GitHub.Issue.Comment.Create(_context.Owner, _context.Repo, pullRequest.Number, body);
        }

        return valid;
    }

    private async Task<bool> VerifyBranchUpToDateAsync(PullRequest pullRequest,
        EnvironmentSettings environmentSettings)
    {
        var compare = await _context.GitHub.Repository.Commit.Compare(_context.Owner, _context.Repo,
            pullRequest.Base.Ref, pullRequest.Head.Ref);

        var pull = pullRequest;
        var valid = compare.BehindBy == 0;
        if (!valid)
        {
            _logger.LogInformation(
                "Branch {Branch} is behind by {BehindBy} commits.\n    Mergeable: {Mergeable}\n    State:     {State}",
                pull.Head.Ref, compare.BehindBy, pull.Mergeable, pull.MergeableState?.StringValue);

            var isUpdatable = await TryUpdateBranchWithMainAsync(pull, environmentSettings);
            if (!isUpdatable)
            {
                _logger.LogDebug("Attempting to get latest pull request merge status.");
                pull = await _pullRequestService.GetAsync(pull.Number);
                isUpdatable = await TryUpdateBranchWithMainAsync(pull, environmentSettings);
            }

            if (!isUpdatable)
            {
                _logger.LogWarning("This branch is out-of-date with the base branch and there are merge conflicts.");

                var body = _templateService.Render(TemplateService.PullRequestMergeConflicts, new
                {
                    owner = _context.Owner,
                    repo = _context.Repo,
                    environment = environmentSettings,
                    badge = Settings.Badge,
                    prdeployPortalUrl = Settings.PrdeployPortalUrl
                });

                await _context.GitHub.Issue.Comment.Create(_context.Owner, _context.Repo, pull.Number, body);

                var user = await _context.GitHub.User.Get(pull.User.Login);
                var slackUser = await _slackService.LookupUserAsync(user);

                var slackBody = _templateService.Render(TemplateService.PullRequestMergeConflictsJson, new
                {
                    environment = environmentSettings,
                    owner = _context.Owner,
                    repo = _context.Repo,
                    pull = TemplateService.Pull(pull),
                    slackUser
                });

                await _slackService.PostMessageAsync(SlackChannel.Deploy, slackBody);
            }
        }

        return valid;
    }

    private async Task<bool> TryUpdateBranchWithMainAsync(PullRequest pullRequest,
        EnvironmentSettings environmentSettings)
    {
        if (pullRequest.Mergeable != true
            && !ValidMergeableStates.Contains(pullRequest.MergeableState?.StringValue ?? string.Empty))
        {
            return false;
        }

        _logger.LogInformation("Updating branch with main.");

        // Octokit.NET does not wrap this endpoint.
        // SourceRef: https://docs.github.com/en/rest/pulls/pulls#update-a-pull-request-branch
        var response = await _context.GitHub.Connection.Put<UpdateBranchResponse>(
            new Uri($"repos/{_context.Owner}/{_context.Repo}/pulls/{pullRequest.Number}/update-branch",
                UriKind.Relative),
            new { });

        var body = _templateService.Render(TemplateService.PullRequestUpdated, new
        {
            owner = _context.Owner,
            repo = _context.Repo,
            environment = environmentSettings,
            badge = Settings.Badge,
            prdeployPortalUrl = Settings.PrdeployPortalUrl,
            updateMessage = response?.Body?.Message
        });

        await _context.GitHub.Issue.Comment.Create(_context.Owner, _context.Repo, pullRequest.Number, body);

        return true;
    }

    private async Task<Build> ExtractBuildAsync(Regex workflowRegex, long runId)
    {
        _logger.LogDebug("Getting the workflow run with ID: {RunId}", runId);
        var run = await _context.GitHub.Actions.Workflows.Runs.Get(_context.Owner, _context.Repo, runId);

        var workflowPath = run.Path ?? string.Empty;
        var match = workflowRegex.Match(workflowPath);
        var name = match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            name = workflowPath;
            _logger.LogWarning("workflow_regex {Regex} did not match workflow path: {Path}", workflowRegex,
                workflowPath);
        }

        var version = await ExtractVersionAsync(runId);
        _logger.LogDebug("Found version: {Version}", version);

        return new Build
        {
            RunId = runId,
            Name = name,
            Version = version,
            Title = run.DisplayTitle,
            Url = run.HtmlUrl
        };
    }

    private async Task<string> ExtractVersionAsync(long runId)
    {
        try
        {
            // SourceRef: https://github.com/dawidd6/action-download-artifact/blob/master/main.js
            _logger.LogDebug("Listing artifacts from Run ID: {RunId}", runId);
            var artifacts = await _context.GitHub.Actions.Artifacts.ListWorkflowArtifacts(_context.Owner,
                _context.Repo, runId, new ListArtifactsRequest());

            var detailsArtifact = artifacts.Artifacts.FirstOrDefault(a => a.Name == BuildDetailsArtifact)
                                  ?? throw new InvalidOperationException(
                                      "Artifact build-details not found on triggering workflow.");

            _logger.LogDebug("Getting artifact with ID: {ArtifactId}", detailsArtifact.Id);
            await using var zip = await _context.GitHub.Actions.Artifacts.DownloadArtifact(_context.Owner,
                _context.Repo, detailsArtifact.Id, "zip");

            using var archive = new ZipArchive(zip, ZipArchiveMode.Read);
            var entry = archive.GetEntry(BuildDetailsFile)
                        ?? throw new InvalidOperationException(
                            "Artifact build-details did not contain build-details.json.");

            await using var entryStream = entry.Open();
            var build = await JsonSerializer.DeserializeAsync<BuildDetails>(entryStream, BuildDetailsSerializerOptions);
            return build?.Version ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "The build-details.json file does not exist, make sure your build uses the build-details action");
            _logger.LogWarning("{Message}", ex.Message);
            return string.Empty;
        }
    }

    [GeneratedRegex(@"/runs/([0-9]+)/jobs?/", RegexOptions.IgnoreCase)]
    private static partial Regex RunRegex();
}

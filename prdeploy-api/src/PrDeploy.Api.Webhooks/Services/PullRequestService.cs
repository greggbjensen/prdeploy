using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>
/// Reads and updates the environment and environment lock labels on pull requests.
/// </summary>
public partial class PullRequestService
{
    private const int MaxPageSize = 100;
    private const string LockSuffix = "-lock";

    private readonly WebhookContext _context;
    private readonly EnvironmentUtil _environmentUtil;
    private readonly ILogger<PullRequestService> _logger;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <param name="environmentUtil">Object containing the environment util data used by the operation.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public PullRequestService(WebhookContext context, EnvironmentUtil environmentUtil,
        ILogger<PullRequestService> logger)
    {
        _context = context;
        _environmentUtil = environmentUtil;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the requested value.
    /// </summary>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public async Task<PullRequest> GetAsync(int pullNumber)
    {
        return await _context.GitHub.PullRequest.Get(_context.Owner, _context.Repo, pullNumber);
    }

    /// <summary>
    /// Finds and returns the deployed.
    /// </summary>
    /// <returns>Items matching the supplied criteria.</returns>
    public async Task<IReadOnlyList<PullRequest>> ListDeployedAsync()
    {
        // Get all environments currently in use.
        var pulls = await _context.GitHub.PullRequest.GetAllForRepository(
            _context.Owner,
            _context.Repo,
            new PullRequestRequest
            {
                State = ItemStateFilter.All,
                SortProperty = PullRequestSort.Updated,
                SortDirection = SortDirection.Descending
            },
            new ApiOptions { PageSize = MaxPageSize, PageCount = 1, StartPage = 1 });

        // Check if pull request is already in an environment.
        var environments = _context.Settings.Environments ?? [];
        return pulls
            .Where(p => p.Labels.Any(l =>
                environments.Any(e => string.Equals(l.Name, e.Name, StringComparison.OrdinalIgnoreCase))))
            .ToList();
    }

    /// <summary>
    /// Performs the progress environment operation and returns its result.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="retainLocks">Whether the retain locks option is enabled.</param>
    /// <returns>The collection of items produced by the operation.</returns>
    public async Task<List<string>> ProgressEnvironmentAsync(PullRequest pullRequest, string environment,
        bool retainLocks)
    {
        var lowerEnvironment = environment.ToLowerInvariant();
        var addEnvironments = new List<string> { lowerEnvironment, $"{lowerEnvironment}{LockSuffix}" };

        _logger.LogInformation(
            "Updating pull request {PullNumber} to deployed for {Environment} with retain {RetainLocks}.",
            pullRequest.Number, environment, retainLocks);

        var removeEnvironments = !retainLocks
            ? GetAllEnvironments(pullRequest)
                .Select(e => $"{e}{LockSuffix}")
                .Where(e => !addEnvironments.Contains(e))
                .ToList()
            : [];

        return await UpdateEnvironmentsAsync(pullRequest, addEnvironments, removeEnvironments);
    }

    /// <summary>
    /// Removes environment locks, so other pull requests can use them.  If no environment is given, all are freed.
    /// </summary>
    /// <returns>The normalized environments that are now available.</returns>
    public async Task<List<string>> FreeAsync(PullRequest pullRequest, string? environment = null)
    {
        _logger.LogInformation("Freeing pull request {PullNumber} for {Environment}.",
            pullRequest.Number, string.IsNullOrEmpty(environment) ? "all environments" : environment);

        var removeEnvironments = !string.IsNullOrEmpty(environment)
            ? [$"{environment.ToLowerInvariant()}{LockSuffix}"]
            : (_context.Settings.Environments ?? []).Select(e => $"{e.Name.ToLowerInvariant()}{LockSuffix}").ToList();

        return await UpdateEnvironmentsAsync(pullRequest, [], removeEnvironments);
    }

    /// <summary>
    /// Removes the environment from the associated collection.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>The collection of items produced by the operation.</returns>
    public async Task<List<string>> RemoveEnvironmentAsync(PullRequest pullRequest, string? environment)
    {
        _logger.LogInformation("Removing environment from pull request {PullNumber} for {Environment}.",
            pullRequest.Number, string.IsNullOrEmpty(environment) ? "all environments" : environment);

        var removeEnvironments = !string.IsNullOrEmpty(environment)
            ? [environment.ToLowerInvariant(), $"{environment.ToLowerInvariant()}{LockSuffix}"]
            : (_context.Settings.Environments ?? [])
                .SelectMany(e => new[] { e.Name.ToLowerInvariant(), $"{e.Name.ToLowerInvariant()}{LockSuffix}" })
                .ToList();

        return await UpdateEnvironmentsAsync(pullRequest, [], removeEnvironments);
    }

    /// <summary>
    /// Lists the environments the pull request has locked.  If an environment is given, only it is listed if locked.
    /// </summary>
    /// <param name="normalized">If the environment given is normalized, so dev matches dev2.</param>
    public List<string> GetAllEnvironments(PullRequest pullRequest, string? environment = null,
        bool normalized = false)
    {
        _logger.LogInformation("List environment from pull request {PullNumber} for {Environment}.",
            pullRequest.Number, string.IsNullOrEmpty(environment) ? "all environments" : environment);

        var environments = (_context.Settings.Environments ?? []).Select(e => e.Name.ToLowerInvariant()).ToList();
        var lockLabels = (pullRequest.Labels ?? [])
            .Where(l => l.Name.EndsWith(LockSuffix, StringComparison.Ordinal))
            .Select(l => EnvironmentLockRegex().Replace(l.Name, string.Empty).ToLowerInvariant())
            .Where(environments.Contains)
            .ToList();

        if (!string.IsNullOrEmpty(environment))
        {
            if (!normalized)
            {
                lockLabels = lockLabels
                    .Where(l => string.Equals(l, environment, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                var found = lockLabels.Find(l => l.StartsWith(environment, StringComparison.OrdinalIgnoreCase));
                lockLabels = found != null ? [found] : [];
            }
        }

        return lockLabels;
    }

    /// <summary>
    /// Finds and returns the with environment.
    /// </summary>
    /// <param name="pulls">Object containing the pulls data used by the operation.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="filterPullNumber">Numeric value used for the filter pull number.</param>
    /// <returns>Items matching the supplied criteria.</returns>
    public List<PullRequest> ListWithEnvironment(IEnumerable<PullRequest> pulls, string environment,
        int filterPullNumber = 0)
    {
        return pulls
            .Where(p => p.Labels.Any(l => string.Equals(l.Name, environment, StringComparison.OrdinalIgnoreCase))
                        && p.Number != filterPullNumber)
            .ToList();
    }

    /// <summary>
    /// Retrieves the used environments.
    /// </summary>
    /// <param name="pulls">Object containing the pulls data used by the operation.</param>
    /// <param name="normalizedEnvironment">Deployment environment involved in the operation.</param>
    /// <param name="currentPullNumber">Numeric value used for the current pull number.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public List<string> GetUsedEnvironments(IEnumerable<PullRequest> pulls, string normalizedEnvironment,
        int currentPullNumber)
    {
        return pulls
            // Do not filter out the environment if it is the current pull request.
            .Where(p => p.Number != currentPullNumber)
            // Check only for lock labels because others can be overridden.
            .SelectMany(p => p.Labels
                .Where(l => l.Name.EndsWith(LockSuffix, StringComparison.Ordinal))
                .Select(l => _environmentUtil.GetFromLock(l.Name)))
            .Where(l => l.StartsWith(normalizedEnvironment, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// Updates the pull request body using the supplied inputs.
    /// </summary>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <param name="body">Text used as the body.</param>
    public async Task UpdatePullRequestBodyAsync(int pullNumber, string body)
    {
        await _context.GitHub.PullRequest.Update(_context.Owner, _context.Repo, pullNumber,
            new PullRequestUpdate { Body = body });

        _logger.LogInformation("Updated pull request {PullNumber} body.", pullNumber);
    }

    /// <summary>
    /// Retrieves the environment.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="run">Object containing the run data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public string GetEnvironment(PullRequest? pullRequest, WorkflowRun? run = null)
    {
        var environment = string.Empty;

        if (run?.Name != null)
        {
            var match = EnvironmentTitleRegex().Match(run.Name);
            if (match.Success)
            {
                environment = match.Groups["environment"].Value.ToLowerInvariant();
            }
        }

        if (!string.IsNullOrEmpty(environment))
        {
            return environment;
        }

        if (pullRequest?.Labels == null || pullRequest.Labels.Count == 0)
        {
            return environment;
        }

        var lockLabels = GetAllEnvironments(pullRequest);
        var lowerRelease = _context.Settings.ReleaseEnvironment?.ToLowerInvariant();
        if (lockLabels.Count > 1)
        {
            // Give weight to release environment, stage, then dev if there are multiple locks.
            environment = lockLabels.OrderBy(l =>
            {
                if (!string.IsNullOrEmpty(lowerRelease) && l == lowerRelease)
                {
                    return 1;
                }

                return l.StartsWith("stage", StringComparison.Ordinal) ? 2 : 3;
            }).First();
        }
        else if (lockLabels.Count == 1)
        {
            environment = lockLabels[0];
        }

        return environment;
    }

    /// <summary>
    /// Determines whether the rollback condition holds.
    /// </summary>
    /// <param name="run">Object containing the run data used by the operation.</param>
    /// <returns>True when the requested condition is met; otherwise, false.</returns>
    public bool IsRollback(WorkflowRun run)
    {
        var match = EnvironmentTitleRegex().Match(run.Name ?? string.Empty);
        return match.Success && match.Groups["rollback"].Value == "ROLLBACK";
    }

    /// <summary>
    /// Retrieves the rollback count.
    /// </summary>
    /// <param name="run">Object containing the run data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public int GetRollbackCount(WorkflowRun run)
    {
        var match = EnvironmentTitleRegex().Match(run.Name ?? string.Empty);
        return match.Success && int.TryParse(match.Groups["rollbackCount"].Value, out var count) ? count : 1;
    }

    /// <summary>
    /// Retrieves the rollback pull number.
    /// </summary>
    /// <param name="run">Object containing the run data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public int GetRollbackPullNumber(WorkflowRun run)
    {
        var match = EnvironmentTitleRegex().Match(run.Name ?? string.Empty);
        return match.Success && int.TryParse(match.Groups["pullNumber"].Value, out var pullNumber) ? pullNumber : 0;
    }

    /// <summary>
    /// Retrieves the defaulted environment.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    public string GetDefaultedEnvironment(PullRequest pullRequest, string? environment)
    {
        if (!string.IsNullOrEmpty(environment))
        {
            return environment;
        }

        // Use the correct dev environment already assigned if there is one.
        var defaultEnvironment = _context.Settings.DefaultEnvironment ?? string.Empty;
        var pullEnvironment = GetEnvironment(pullRequest);
        return !string.IsNullOrEmpty(pullEnvironment)
               && pullEnvironment.StartsWith(defaultEnvironment, StringComparison.OrdinalIgnoreCase)
            ? pullEnvironment
            : defaultEnvironment;
    }

    /// <summary>
    /// Updates the environments using the supplied inputs.
    /// </summary>
    /// <param name="pullRequest">Pull request associated with the operation.</param>
    /// <param name="addLabels">Object containing the add labels data used by the operation.</param>
    /// <param name="removeLabels">Object containing the remove labels data used by the operation.</param>
    /// <returns>The value after it has been stored or updated.</returns>
    private async Task<List<string>> UpdateEnvironmentsAsync(PullRequest pullRequest,
        IReadOnlyCollection<string> addLabels, IReadOnlyCollection<string> removeLabels)
    {
        var existingLabels = (pullRequest.Labels ?? []).Select(l => l.Name).ToList();

        var updateLabels = existingLabels
            .Where(l => !removeLabels.Contains(l.ToLowerInvariant()))
            .Concat(addLabels)
            .Distinct()
            .ToArray();

        _logger.LogDebug("Updating pull {PullNumber} labels:\n    Before: {Before}\n    After: {After}",
            pullRequest.Number, string.Join(", ", existingLabels), string.Join(", ", updateLabels));

        var availableEnvironments = existingLabels
            .Where(l => removeLabels.Contains($"{l.ToLowerInvariant()}{LockSuffix}"))
            .Select(_environmentUtil.Normalize)
            .Distinct()
            .ToList();

        await _context.GitHub.Issue.Labels.ReplaceAllForIssue(_context.Owner, _context.Repo, pullRequest.Number,
            updateLabels);

        return availableEnvironments;
    }

    /// <summary>
    /// Performs the environment title regex operation and returns its result.
    /// </summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex(@"^\[(?<environment>\w+\d*)(?:\s+(?<rollback>ROLLBACK)(?:\s+#(?<pullNumber>\d+))?(?:\s+(?<rollbackCount>\d+))?)?\]")]
    private static partial Regex EnvironmentTitleRegex();

    /// <summary>
    /// Performs the environment lock regex operation and returns its result.
    /// </summary>
    /// <returns>The regex produced by the operation.</returns>
    [GeneratedRegex("-lock$", RegexOptions.IgnoreCase)]
    private static partial Regex EnvironmentLockRegex();
}

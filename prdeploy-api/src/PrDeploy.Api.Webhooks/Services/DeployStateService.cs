using Microsoft.Extensions.Logging;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.DeployEnvironments;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>
/// Tracks which service versions are deployed to each environment as deployment values.
/// </summary>
public class DeployStateService
{
    public const string StableEnvironment = "stable";
    private const string DeployStatePrefix = "DEPLOY_STATE_";

    private readonly IDeploymentStore _deploymentStore;
    private readonly WebhookContext _context;
    private readonly ILogger<DeployStateService> _logger;

    /// <summary>
    /// Initializes a deploy State Service with the supplied dependencies.
    /// </summary>
    /// <param name="deploymentStore">Value supplied for deployment Store.</param>
    /// <param name="context">Request or application context used by the operation.</param>
    /// <param name="logger">Logger used to report operational information and errors.</param>
    public DeployStateService(IDeploymentStore deploymentStore, WebhookContext context,
        ILogger<DeployStateService> logger)
    {
        _deploymentStore = deploymentStore;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Synchronizes the requested value with the current source of truth.
    /// </summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    public async Task SyncAsync(string environment)
    {
        var stableState = await GetAsync(StableEnvironment);
        await SetAsync(environment, stableState);
    }

    /// <summary>
    /// Updates the target value using the supplied inputs.
    /// </summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <param name="services">Services to include in the operation.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<DeployState> UpdateAsync(string environment, int pullNumber,
        IReadOnlyCollection<DeployedService> services)
    {
        _logger.LogInformation("Updating deploy state for {Environment} and pull number {PullNumber}.",
            environment, pullNumber);

        var state = await GetAsync(environment);
        state.PullNumber = pullNumber;

        // Filter out any existing services that will be added.
        state.Services = state.Services!
            .Where(s => services.All(i => s.Name != i.Name))
            .Concat(services)
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .ToList();

        await SetAsync(environment, state);
        return state;
    }

    /// <summary>
    /// Retrieves the requested value.
    /// </summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="historical">Number of revisions to move back from the latest value; zero selects the latest revision.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<DeployState> GetAsync(string environment, int historical = 0)
    {
        var name = GetVariableName(environment);
        var value = historical == 0
            ? await _deploymentStore.GetAsync<DeployState?>(_context.Owner, _context.Repo, name)
            : await _deploymentStore.GetHistoricalAsync<DeployState?>(_context.Owner, _context.Repo, name, historical);

        if (value == null)
        {
            value = new DeployState { PullNumber = 0, Services = [] };

            // Only create the current value, a missing historical value must not overwrite it.
            if (historical == 0)
            {
                _logger.LogInformation("Creating deploy state for: {Name}.", name);
                await _deploymentStore.SetAsync(_context.Owner, _context.Repo, name, value);
            }
        }

        // Empty lists are not saved to deployment storage.
        value.Services ??= [];
        return value;
    }

    /// <summary>
    /// Compares the requested value and returns the differences.
    /// </summary>
    /// <param name="sourceEnvironment">Source environment or value for the operation.</param>
    /// <param name="targetEnvironment">Target environment or value for the operation.</param>
    /// <param name="sourceHistorical">Value supplied for source Historical.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<DeployState> DiffAsync(string sourceEnvironment, string targetEnvironment,
        int sourceHistorical = 0)
    {
        var sourceState = await GetAsync(sourceEnvironment, sourceHistorical);
        var targetState = await GetAsync(targetEnvironment);

        var targetStateLookup = targetState.Services!.ToDictionary(s => s.Name, s => s);

        return new DeployState
        {
            PullNumber = sourceState.PullNumber,
            Services = sourceState.Services!
                .Where(source => !targetStateLookup.TryGetValue(source.Name, out var target)
                                 || target.Version != source.Version)
                .ToList()
        };
    }

    /// <summary>
    /// Stores or updates the supplied value.
    /// </summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="state">Value supplied for state.</param>
    private async Task SetAsync(string environment, DeployState state)
    {
        var name = GetVariableName(environment);
        await _deploymentStore.SetAsync(_context.Owner, _context.Repo, name, state);
    }

    /// <summary>
    /// Retrieves the Variable Name.
    /// </summary>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <returns>The value produced by this operation.</returns>
    private static string GetVariableName(string environment) =>
        $"{DeployStatePrefix}{environment.ToUpperInvariant()}";
}

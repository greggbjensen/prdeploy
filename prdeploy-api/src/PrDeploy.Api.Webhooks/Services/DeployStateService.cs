using Microsoft.Extensions.Logging;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.DeployEnvironments;

namespace PrDeploy.Api.Webhooks.Services;

/// <summary>
/// Tracks which service versions are deployed to each environment in AWS Parameter Store.
/// </summary>
public class DeployStateService
{
    public const string StableEnvironment = "stable";
    private const string DeployStatePrefix = "DEPLOY_STATE_";

    private readonly IParameterStore _parameterStore;
    private readonly WebhookContext _context;
    private readonly ILogger<DeployStateService> _logger;

    public DeployStateService(IParameterStore parameterStore, WebhookContext context,
        ILogger<DeployStateService> logger)
    {
        _parameterStore = parameterStore;
        _context = context;
        _logger = logger;
    }

    public async Task SyncAsync(string environment)
    {
        var stableState = await GetAsync(StableEnvironment);
        await SetAsync(environment, stableState);
    }

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

    public async Task<DeployState> GetAsync(string environment, int historical = 0)
    {
        var name = GetVariableName(environment);
        var value = historical == 0
            ? await _parameterStore.GetAsync<DeployState?>(_context.Owner, _context.Repo, name)
            : await _parameterStore.GetHistoricalAsync<DeployState?>(_context.Owner, _context.Repo, name, historical);

        if (value == null)
        {
            value = new DeployState { PullNumber = 0, Services = [] };

            // Only create the current value, a missing historical value must not overwrite it.
            if (historical == 0)
            {
                _logger.LogInformation("Creating deploy state for: {Name}.", name);
                await _parameterStore.SetAsync(_context.Owner, _context.Repo, name, value);
            }
        }

        // Empty lists are not saved to parameter store.
        value.Services ??= [];
        return value;
    }

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

    private async Task SetAsync(string environment, DeployState state)
    {
        var name = GetVariableName(environment);
        await _parameterStore.SetAsync(_context.Owner, _context.Repo, name, state);
    }

    private static string GetVariableName(string environment) =>
        $"{DeployStatePrefix}{environment.ToUpperInvariant()}";
}

using FluentValidation;
using PrDeploy.Api.Business.Services.Interfaces;
using Octokit;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.General.Inputs;
using Environment = PrDeploy.Api.Models.DeployEnvironments.Environment;

namespace PrDeploy.Api.Business.Services;

/// <summary>
/// Coordinates operations for the deploy Environment Service and its collaborators.
/// </summary>
public class DeployEnvironmentService : IDeployEnvironmentService
{
    private const string DeployStatePrefix = "DEPLOY_STATE_";
    private const string? DefaultEnvironmentColor = "#6c757d";
    private readonly IGitHubClient _gitHubClient;
    private readonly IDeploySettingsService _deploySettingsService;
    private readonly IPullRequestService _pullRequestService;
    private readonly IGitHubSecurity _gitHubSecurity;
    private readonly IDeploymentStore _deploymentStore;
    private readonly IValidator<DeployStateComparisonInput> _deployStateComparisonInputValidator;
    private readonly IValidator<RepoQueryInput> _environmentsInputValidator;

    /// <summary>
    /// Initializes a deploy Environment Service with the supplied dependencies.
    /// </summary>
    /// <param name="gitHubClient">Value supplied for git Hub Client.</param>
    /// <param name="deploySettingsService">Value supplied for deploy Settings Service.</param>
    /// <param name="pullRequestService">Value supplied for pull Request Service.</param>
    /// <param name="gitHubSecurity">Value supplied for git Hub Security.</param>
    /// <param name="deploymentStore">Value supplied for deployment Store.</param>
    /// <param name="deployStateComparisonInputValidator">Value supplied for deploy State Comparison Input Validator.</param>
    /// <param name="environmentsInputValidator">Value supplied for environments Input Validator.</param>
    public DeployEnvironmentService(IGitHubClient gitHubClient, IDeploySettingsService deploySettingsService,
        IPullRequestService pullRequestService, IGitHubSecurity gitHubSecurity,
        IDeploymentStore deploymentStore, IValidator<DeployStateComparisonInput> deployStateComparisonInputValidator,
        IValidator<RepoQueryInput> environmentsInputValidator)
    {
        _environmentsInputValidator = environmentsInputValidator;
        _gitHubClient = gitHubClient;
        _deploySettingsService = deploySettingsService;
        _pullRequestService = pullRequestService;
        _gitHubSecurity = gitHubSecurity;
        _deploymentStore = deploymentStore;
        _deployStateComparisonInputValidator = deployStateComparisonInputValidator;
    }

    /// <summary>
    /// Finds and returns the matching items.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<List<DeployEnvironment>> ListAsync(RepoQueryInput input)
    {
        var deployEnvironments = new List<DeployEnvironment>();

        var repoSettings = await _deploySettingsService.GetMergedAsync(input.Owner, input.Repo);
        foreach (var environment in repoSettings.Environments!)
        {
            var deployEnvironment = new DeployEnvironment
            {
                Name = environment.Name,
                Url = environment.Url,
                Color = environment.Color
            };

            if (string.IsNullOrEmpty(deployEnvironment.Color))
            {
                deployEnvironment.Color = DefaultEnvironmentColor;
            }

            var issues = await _gitHubClient.Issue.GetAllForRepository(
                input.Owner,
                input.Repo,
                new RepositoryIssueRequest
                {
                    Labels = { environment.Name },
                    State = ItemStateFilter.All,
                    SortDirection = SortDirection.Descending,
                    SortProperty = IssueSort.Updated
                },
                new ApiOptions { PageCount = 1, PageSize = 1 });
            var issue = issues.FirstOrDefault();
            deployEnvironment.PullRequest = Map.PullRequest(issue);

            if (issue != null)
            {
                var lockLabel = $"{environment.Name}-lock";
                deployEnvironment.Locked = issue.Labels.Any(
                    l => string.Equals(l.Name, lockLabel, StringComparison.OrdinalIgnoreCase));
            }

            deployEnvironments.Add(deployEnvironment);
        }

        return deployEnvironments;
    }

    /// <summary>
    /// Finds and returns the Environments.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<List<Environment>> ListEnvironmentsAsync(RepoQueryInput input)
    {
        await _environmentsInputValidator.ValidateAndThrowAsync(input);
        await _gitHubSecurity.GuardRepoAsync(input.Owner, input.Repo);

        var repoSettings = await _deploySettingsService.GetMergedAsync(input.Owner, input.Repo);
        var environments = repoSettings.Environments!.Select(e => new Environment
        {
            Name = e.Name,
            Url = e.Url
        }).ToList();

        return environments;
    }

    /// <summary>
    /// Releases the requested value so it can be used again.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<StatusResponse> FreeAsync(PullDeployInput input)
    {
        return await _pullRequestService.AddCommentCommandAsync(input.Owner, input.Repo, input.PullNumber, 
            $"/free {input.Environment.ToLower()}");
    }

    /// <summary>
    /// Performs the deploy operation and returns its result.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<StatusResponse> DeployAsync(EnvironmentDeployInput input)
    {
        var retainString = input.Retain.GetValueOrDefault() ? " --retain" : string.Empty;
        var forceString = input.Force.GetValueOrDefault() ? " --force" : string.Empty;
        return await _pullRequestService.AddCommentCommandAsync(input.Owner, input.Repo, input.PullNumber, 
            $"/deploy {input.Environment.ToLower()}{forceString}{retainString}");
    }

    /// <summary>
    /// Restores the requested value to a previous state.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<StatusResponse> RollbackAsync(RollbackInput input)
    {
        var countString = input.Count > 1 ? $" {input.Count}" : string.Empty;
        return await _pullRequestService.AddCommentCommandAsync(input.Owner, input.Repo, input.PullNumber, 
            $"/rollback {input.Environment.ToLower()}{countString}");
    }

    /// <summary>
    /// Compares the Deploy State and returns the differences.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    public async Task<DeployStateComparison> CompareDeployStateAsync(DeployStateComparisonInput input)
    {
        await _deployStateComparisonInputValidator.ValidateAndThrowAsync(input);

        // Because we do not make a GitHub call here, we need to secure the repo access.
        await _gitHubSecurity.GuardRepoAsync(input.Owner, input.Repo);

        var sourceStateName = DeployStatePrefix + input.SourceEnvironment.ToUpperInvariant();
        var targetStateName = DeployStatePrefix + input.TargetEnvironment.ToUpperInvariant();
        var sourceState = await GetOrCreateDeployState(input.Owner, input.Repo, sourceStateName);
        var targetState = await GetOrCreateDeployState(input.Owner, input.Repo, targetStateName);

        var targetServiceLookUp = targetState.Services!.ToDictionary(s => s.Name, s => s, 
            StringComparer.OrdinalIgnoreCase);
        var remainingServices = new List<DeployedService>(targetState.Services!);
        var serviceComparisons = new List<ServiceComparison>(sourceState.Services!.Count);
        foreach (var sourceService in sourceState.Services)
        {
            var serviceComparison = new ServiceComparison
            {
                Name = sourceService.Name,
                SourceRunId = sourceService.RunId,
                SourceVersion = sourceService.Version
            };

            if (targetServiceLookUp.TryGetValue(sourceService.Name, out var targetService))
            {
                serviceComparison.TargetRunId = targetService.RunId;
                serviceComparison.TargetVersion = targetService.Version;
                remainingServices.Remove(targetService);
            }

            serviceComparisons.Add(serviceComparison);
        }

        foreach (var targetService in remainingServices)
        {
            var serviceComparison = new ServiceComparison
            {
                Name = targetService.Name,
                TargetRunId = targetService.RunId,
                TargetVersion = targetService.Version
            };

            serviceComparisons.Add(serviceComparison);
        }

        var stateComparison = new DeployStateComparison
        {
            SourceEnvironment = input.SourceEnvironment,
            SourcePullNumber = sourceState.PullNumber,
            TargetEnvironment = input.TargetEnvironment,
            TargetPullNumber = targetState.PullNumber,
            ServiceComparisons = serviceComparisons
        };

        return stateComparison;
    }

    /// <summary>
    /// Retrieves the Or Create Deploy State.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <returns>The value produced by this operation.</returns>
    private async Task<DeployState> GetOrCreateDeployState(string owner, string repo, string name)
    {
        var state = await _deploymentStore.GetAsync<DeployState?>(owner, repo, name);
        if (state == null)
        {
            state = new DeployState
            {
                PullNumber = 0,
                Services = new List<DeployedService>()
            };
            await _deploymentStore.SetAsync(owner, repo, name, state);
        }

        if (state.Services == null)
        {
            state.Services = new List<DeployedService>();
        }

        return state;
    }
}

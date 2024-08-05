using Amazon.SimpleSystemsManagement;
using FluentValidation;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using Octokit;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.DeployEnvironments;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.General;

namespace PrDeploy.Api.Business.Services;

public class DeployEnvironmentService : IDeployEnvironmentService
{
    private const string DeployStatePrefix = "DEPLOY_STATE_";
    private readonly IGitHubClient _gitHubClient;
    private readonly IRepoSettingsService _repoSettingsService;
    private readonly IPullRequestService _pullRequestService;
    private readonly IRepositorySecurity _repositorySecurity;
    private readonly IParameterStore _parameterStore;
    private readonly IValidator<DeployStateComparisonInput> _deployStateComparisonInputValidator;

    public DeployEnvironmentService(IGitHubClient gitHubClient, IRepoSettingsService repoSettingsService,
        IPullRequestService pullRequestService, IRepositorySecurity repositorySecurity,
        IParameterStore parameterStore, IValidator<DeployStateComparisonInput> deployStateComparisonInputValidator)
    {
        _gitHubClient = gitHubClient;
        _repoSettingsService = repoSettingsService;
        _pullRequestService = pullRequestService;
        _repositorySecurity = repositorySecurity;
        _parameterStore = parameterStore;
        _deployStateComparisonInputValidator = deployStateComparisonInputValidator;
    }

    public async Task<List<DeployEnvironment>> ListAsync(string owner, string repo)
    {
        var deployEnvironments = new List<DeployEnvironment>();

        var repoSettings = await _repoSettingsService.GetAsync(owner, repo);
        var labels = await _gitHubClient.Issue.Labels.GetAllForRepository(owner, repo);
        var environmentColors = labels.ToDictionary(l => l.Name, l => l.Color, StringComparer.OrdinalIgnoreCase);
        foreach (var environment in repoSettings.Environments!)
        {
            var deployEnvironment = new DeployEnvironment
            {
                Name = environment.Name,
                Url = environment.Url
            };

            if (environmentColors.TryGetValue(environment.Name, out string color))
            {
                deployEnvironment.Color = color;
            }

            var issues = await _gitHubClient.Issue.GetAllForRepository(
                owner,
                repo,
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

    public async Task<StatusResponse> FreeAsync(string owner, string repo, string environment, int pullRequestNumber)
    {
        return await _pullRequestService.AddCommentCommandAsync(owner, repo, pullRequestNumber, $"/free {environment.ToLower()}");
    }

    public async Task<StatusResponse> DeployAsync(string owner, string repo, string environment, int pullRequestNumber,
        bool force, bool retain)
    {
        var retainString = retain ? " --retain" : string.Empty;
        var forceString = force ? " --force" : string.Empty;
        return await _pullRequestService.AddCommentCommandAsync(owner, repo, pullRequestNumber, $"/deploy {environment.ToLower()}{forceString}{retainString}");
    }

    public async Task<StatusResponse> RollbackAsync(string owner, string repo, string environment, int pullRequestNumber,
        int count)
    {
        var countString = count > 1 ? $" {count}" : string.Empty;
        return await _pullRequestService.AddCommentCommandAsync(owner, repo, pullRequestNumber, $"/rollback {environment.ToLower()}{countString}");
    }

    public async Task<DeployStateComparison> CompareDeployStateAsync(DeployStateComparisonInput input)
    {
        await _deployStateComparisonInputValidator.ValidateAndThrowAsync(input);

        // Because we do not make a GitHub call here, we need to secure the repo access.
        await _repositorySecurity.GuardAsync(input.Owner, input.Repo);

        var sourceStateName = DeployStatePrefix + input.SourceEnvironment.ToUpperInvariant();
        var targetStateName = DeployStatePrefix + input.TargetEnvironment.ToUpperInvariant();
        var sourceState = await _parameterStore.GetAsync<DeployState>(input.Owner, input.Repo, sourceStateName);
        var targetState = await _parameterStore.GetAsync<DeployState>(input.Owner, input.Repo, targetStateName);

        var targetServiceLookUp = targetState.Services.ToDictionary(s => s.Name, s => s, 
            StringComparer.OrdinalIgnoreCase);
        var remainingServices = new List<DeployedService>(targetState.Services);
        var serviceComparisons = new List<ServiceComparison>(sourceState.Services.Count);
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
}

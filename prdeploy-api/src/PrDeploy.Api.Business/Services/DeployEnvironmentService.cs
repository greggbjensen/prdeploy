using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using Octokit;

namespace PrDeploy.Api.Business.Services;

public class DeployEnvironmentService : IDeployEnvironmentService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly IRepoSettingsService _repoSettingsService;
    private readonly IPullRequestService _pullRequestService;

    public DeployEnvironmentService(IGitHubClient gitHubClient, IRepoSettingsService repoSettingsService,
        IPullRequestService pullRequestService)
    {
        _gitHubClient = gitHubClient;
        _repoSettingsService = repoSettingsService;
        _pullRequestService = pullRequestService;
    }

    public async Task<List<DeployEnvironment>> ListAsync(string owner, string repo)
    {
        var deployEnvironments = new List<DeployEnvironment>();

        var repoSettings = await _repoSettingsService.GetAsync(owner, repo);
        var labels = await _gitHubClient.Issue.Labels.GetAllForRepository(owner, repo);
        var environmentColors = labels.ToDictionary(l => l.Name, l => l.Color, StringComparer.OrdinalIgnoreCase);
        foreach (var environment in repoSettings.Environments)
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
}

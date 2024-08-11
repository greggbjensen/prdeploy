using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using Octokit;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.DeployQueues;
using PrDeploy.Api.Models.DeployQueues.Inputs;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Business.Services;

public class DeployQueueService : IDeployQueueService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly IDeploySettingsService _deploySettingsService;
    private readonly IParameterStore _parameterStore;

    public DeployQueueService(IGitHubClient gitHubClient, IDeploySettingsService deploySettingsService, IParameterStore parameterStore)
    {
        _gitHubClient = gitHubClient;
        _deploySettingsService = deploySettingsService;
        _parameterStore = parameterStore;
    }

    public async Task<DeployQueue> UpdateAsync(DeployQueueUpdateInput input)
    {
        var value = new List<int>();
        if (input.PullNumbers?.Any() == true)
        {
            await AddCommentForNewEntriesAsync(input.Owner, input.Repo, input.Environment, input.PullNumbers);
            value = input.PullNumbers;
        }

        var environmentSettings = await _deploySettingsService.GetEnvironmentAsync(input.Owner, input.Repo, input.Environment);
        await SetPullNumbersAsync(input.Owner, input.Repo, environmentSettings.Queue!, value);

        var deployQueue = await GetQueueAsync(input.Owner, input.Repo, environmentSettings);
        return deployQueue;
    }

    public async Task<List<DeployQueue>> ListAsync(RepoQueryInput input)
    {
        // Get queues in parallel.
        var environmentSettings = await _deploySettingsService.GetQueueEnvironmentsAsync(input.Owner, input.Repo);
        var queueTasks = environmentSettings.Select(e => GetQueueAsync(input.Owner, input.Repo, e));
        var queues = await Task.WhenAll(queueTasks);

        return queues.ToList();
    }

    private async Task AddCommentForNewEntriesAsync(string owner, string repo, string environment,
        List<int> pullNumbers)
    {
        // Compare against current queue and add command comment for new ones.
        var repoSettings = await _deploySettingsService.GetMergedAsync(owner, repo);
        var environmentSettings = _deploySettingsService.GetEnvironment(owner, repo, environment, repoSettings);
        var currentNumbers = await GetPullNumbersAsync(owner, repo, environmentSettings!.Queue);

        var newNumbers = pullNumbers.Where(n => !currentNumbers.Contains(n)).ToArray();
        if (newNumbers.Any())
        {
            var command = environment.StartsWith(repoSettings.DefaultEnvironment!, StringComparison.OrdinalIgnoreCase)
                ? @"/deploy"
                : $@"/deploy {environment}";
            var tasks = newNumbers.Select(n => AddPullRequestCommand(owner, repo, n, command)).ToArray();
            await Task.WhenAll(tasks);
        }
    }

    private async Task<DeployQueue> GetQueueAsync(string owner, string repo, EnvironmentSettings environmentSettings)
    {
        var queue = new DeployQueue
        {
            Environment = environmentSettings.Name
        };

        var pullNumbers = await GetPullNumbersAsync(owner, repo, environmentSettings.Queue);
        foreach (var pullNumber in pullNumbers)
        {
            // Do not do these in parallel to avoid hitting API limit.
            var response = await _gitHubClient.PullRequest.Get(owner, repo, pullNumber);
            var pullRequest = Map.PullRequest(response);
            if (pullRequest != null)
            {
                queue.PullRequests.Add(pullRequest);
            }
        }

        return queue;
    }

    private async Task<List<int>> GetPullNumbersAsync(string owner, string repo, string queueName)
    {
        var pullNumbers = await _parameterStore.GetAsync<List<int>?>(owner, repo, queueName);

        // Set a default empty value if not found.
        if (pullNumbers == null)
        {
            pullNumbers = new List<int>();
            await SetPullNumbersAsync(owner, repo, queueName, pullNumbers);
        }

        return pullNumbers;
    }

    private async Task SetPullNumbersAsync(string owner, string repo, string queueName, List<int> value)
    {
        await _parameterStore.SetAsync(owner, repo, queueName, value);
    }

    private async Task<StatusResponse> AddPullRequestCommand(string owner, string repo, int pullNumber,
        string command)
    {
        var comment = $@"
{command}
";
        await _gitHubClient.Issue.Comment.Create(
            owner,
            repo,
            pullNumber,
            comment
        );

        return new StatusResponse();
    }
}

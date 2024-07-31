using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using PrDeploy.Api.Business.Models.Settings;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using Octokit;
using YamlDotNet.Serialization;

namespace PrDeploy.Api.Business.Services;

public class DeployQueueService : IDeployQueueService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly IAmazonSimpleSystemsManagement _amazonSsm;
    private readonly IRepoSettingsService _repoSettingsService;

    public DeployQueueService(IGitHubClient gitHubClient, IAmazonSimpleSystemsManagement amazonSsm, IRepoSettingsService repoSettingsService)
    {
        _gitHubClient = gitHubClient;
        _amazonSsm = amazonSsm;
        _repoSettingsService = repoSettingsService;
    }

    public async Task<DeployQueue> UpdateAsync(string owner, string repo, string environment,
        List<int>? pullRequestNumbers)
    {
        var value = new List<int>();
        if (pullRequestNumbers?.Any() == true)
        {
            await AddCommentForNewEntriesAsync(owner, repo, environment, pullRequestNumbers);
            value = pullRequestNumbers;
        }

        var environmentSettings = await _repoSettingsService.GetEnvironmentAsync(owner, repo, environment);
        await SetPullNumbersAsync(owner, repo, environmentSettings.Queue, value);

        var deployQueue = await GetQueueAsync(owner, repo, environmentSettings);
        return deployQueue;
    }

    public async Task<List<DeployQueue>> ListAsync(string owner, string repo)
    {
        // Get queues in parallel.
        var environmentSettings = await _repoSettingsService.GetQueueEnvironmentsAsync(owner, repo);
        var queueTasks = environmentSettings.Select(e => GetQueueAsync(owner, repo, e));
        var queues = await Task.WhenAll(queueTasks);

        return queues.ToList();
    }

    private static string GetParameterName(string owner, string repo, string? queueName) =>
        $"/prdeploy/{owner}/{repo}/{queueName}";

    private async Task AddCommentForNewEntriesAsync(string owner, string repo, string environment,
        List<int> pullRequestNumbers)
    {
        // Compare against current queue and add command comment for new ones.
        var repoSettings = await _repoSettingsService.GetAsync(owner, repo);
        var environmentSettings = _repoSettingsService.GetEnvironment(owner, repo, environment, repoSettings);
        var currentNumbers = await GetPullNumbersAsync(owner, repo, environmentSettings.Queue);

        var newNumbers = pullRequestNumbers.Where(n => !currentNumbers.Contains(n)).ToArray();
        if (newNumbers.Any())
        {
            var command = environment.StartsWith(repoSettings.DefaultEnvironment, StringComparison.OrdinalIgnoreCase)
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

    private async Task<List<int>> GetPullNumbersAsync(string owner, string repo, string? queueName)
    {
        List<int>? pullNumbers = null;

        var request = new GetParameterRequest
        {
            Name = GetParameterName(owner, repo, queueName),
        };
        try
        {
            var parameterResponse = await _amazonSsm.GetParameterAsync(request);
            var value = parameterResponse.Parameter.Value;
            var deserializer = new DeserializerBuilder().Build();
            pullNumbers = deserializer.Deserialize<List<int>>(value);
        }
        catch (ParameterNotFoundException)
        {
            // Set a default empty value if not found.
            pullNumbers = new List<int>();
            await SetPullNumbersAsync(owner, repo, queueName, pullNumbers);
        }

        return pullNumbers ?? new List<int>();
    }

    private async Task SetPullNumbersAsync(string owner, string repo, string? queueName, List<int> value)
    {
        var serializer = new SerializerBuilder().Build();
        var yaml = serializer.Serialize(value);
        var request = new PutParameterRequest
        {
            Name = GetParameterName(owner, repo, queueName),
            Type = ParameterType.String,
            Value = yaml,
            Overwrite = true,
            Tier = ParameterTier.Standard
        };
        await _amazonSsm.PutParameterAsync(request);
    }

    private async Task<StatusResponse> AddPullRequestCommand(string owner, string repo, int pullRequestNumber,
        string command)
    {
        var comment = $@"
{command}
";
        await _gitHubClient.Issue.Comment.Create(
            owner,
            repo,
            pullRequestNumber,
            comment
        );

        return new StatusResponse();
    }
}

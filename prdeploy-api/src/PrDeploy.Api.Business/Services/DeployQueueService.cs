using Microsoft.Extensions.Logging;
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

/// <summary>
/// Coordinates deploy queue operations and their collaborators.
/// </summary>
public class DeployQueueService : IDeployQueueService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly IDeploySettingsService _deploySettingsService;
    private readonly IDeploymentStore _deploymentStore;
    private readonly ILogger<DeployQueueService> _logger;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="gitHubClient">Object containing the git hub client data used by the operation.</param>
    /// <param name="deploySettingsService">Object containing the deploy settings service data used by the operation.</param>
    /// <param name="deploymentStore">Object containing the deployment store data used by the operation.</param>
    /// <param name="logger">Logger used to report operational activity.</param>
    public DeployQueueService(IGitHubClient gitHubClient, IDeploySettingsService deploySettingsService, IDeploymentStore deploymentStore,
        ILogger<DeployQueueService> logger)
    {
        _gitHubClient = gitHubClient;
        _deploySettingsService = deploySettingsService;
        _deploymentStore = deploymentStore;
        _logger = logger;
    }

    /// <summary>
    /// Updates the target value using the supplied inputs.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value after it has been stored or updated.</returns>
    public async Task<DeployQueue> UpdateAsync(DeployQueueUpdateInput input)
    {
        _logger.LogInformation("Updating {Environment} deploy queue in {Owner}/{Repo} to {Count} pull requests.",
            input.Environment, input.Owner, input.Repo, input.PullNumbers?.Count ?? 0);
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

    /// <summary>
    /// Finds and returns the matching items.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>Items matching the supplied criteria.</returns>
    public async Task<List<DeployQueue>> ListAsync(RepoQueryInput input)
    {
        _logger.LogDebug("Listing deploy queues for {Owner}/{Repo}.", input.Owner, input.Repo);

        // Get queues in parallel.
        var environmentSettings = await _deploySettingsService.GetQueueEnvironmentsAsync(input.Owner, input.Repo);
        var queueTasks = environmentSettings.Select(e => GetQueueAsync(input.Owner, input.Repo, e));
        var queues = await Task.WhenAll(queueTasks);

        return queues.ToList();
    }

    /// <summary>
    /// Adds the comment for new entries to the associated collection.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="environment">Deployment environment involved in the operation.</param>
    /// <param name="pullNumbers">Object containing the pull numbers data used by the operation.</param>
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
            _logger.LogInformation(
                "Queueing {Count} new pull requests for {Environment} in {Owner}/{Repo}: {PullNumbers}.",
                newNumbers.Length, environment, owner, repo, newNumbers);
            var command = environment.StartsWith(repoSettings.DefaultEnvironment!, StringComparison.OrdinalIgnoreCase)
                ? @"/deploy"
                : $@"/deploy {environment}";
            var tasks = newNumbers.Select(n => AddPullRequestCommand(owner, repo, n, command)).ToArray();
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Retrieves the queue.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="environmentSettings">Object containing the environment settings data used by the operation.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
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

    /// <summary>
    /// Retrieves the pull numbers.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="queueName">Text used as the queue name.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    private async Task<List<int>> GetPullNumbersAsync(string owner, string repo, string queueName)
    {
        var pullNumbers = await _deploymentStore.GetAsync<List<int>?>(owner, repo, queueName);

        // Set a default empty value if not found.
        if (pullNumbers == null)
        {
            _logger.LogDebug("Creating empty deploy queue {QueueName} for {Owner}/{Repo}.", queueName, owner, repo);
            pullNumbers = new List<int>();
            await SetPullNumbersAsync(owner, repo, queueName, pullNumbers);
        }

        return pullNumbers;
    }

    /// <summary>
    /// Stores or updates the pull numbers.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="queueName">Text used as the queue name.</param>
    /// <param name="value">Value to store or process.</param>
    private async Task SetPullNumbersAsync(string owner, string repo, string queueName, List<int> value)
    {
        await _deploymentStore.SetAsync(owner, repo, queueName, value);
    }

    /// <summary>
    /// Adds the pull request command to the associated collection.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <param name="command">Text used as the command.</param>
    /// <returns>The status response produced by the operation.</returns>
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

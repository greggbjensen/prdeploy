using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using LinqKit;
using Microsoft.Extensions.Logging;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using Octokit;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.PullRequests.Inputs;
using PullRequest = PrDeploy.Api.Models.PullRequests.PullRequest;

namespace PrDeploy.Api.Business.Services;

/// <summary>Coordinates pull request operations and their collaborators.</summary>
public partial class PullRequestService : IPullRequestService
{
    private readonly IGitHubClient _client;
    private readonly IGitHubSecurity _gitHubSecurity;
    private readonly ILogger<PullRequestService> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="client">Object containing the client data used by the operation.</param>
    /// <param name="gitHubSecurity">Guard that limits access to repositories in allowed organizations.</param>
    /// <param name="logger">Logger used to report operational activity.</param>
    public PullRequestService(IGitHubClient client, IGitHubSecurity gitHubSecurity, ILogger<PullRequestService> logger)
    {
        _client = client;
        _gitHubSecurity = gitHubSecurity;
        _logger = logger;
    }

    /// <summary>Finds and returns the open pull requests.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>Items matching the supplied criteria.</returns>
    public async Task<List<PullRequest>> ListOpenPullRequestsAsync(OpenPullRequestInput input)
    {
        await _gitHubSecurity.GuardRepoAsync(input.Owner, input.Repo);
        _logger.LogDebug("Listing open pull requests for {Owner}/{Repo} (search provided {HasSearch}).",
            input.Owner, input.Repo, !string.IsNullOrWhiteSpace(input.Search));

        // Cannot search with this API, so we do it locally.
        var results = await _client.PullRequest.GetAllForRepository(
            input.Owner,
            input.Repo,
            new PullRequestRequest { State = ItemStateFilter.Open },
            new ApiOptions { PageCount = 1, PageSize = 100 });

        var predicate = PredicateBuilder.New<Octokit.PullRequest>(true);
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            predicate.And(p => p.Title.Contains(input.Search, StringComparison.OrdinalIgnoreCase)
                               || p.Number.ToString().Contains(input.Search)
                               || (p.User.Name != null && p.User.Name.Contains(input.Search, StringComparison.OrdinalIgnoreCase))
                               || p.User.Login.Contains(input.Search, StringComparison.OrdinalIgnoreCase));
        }

        var pullRequests = results.Where(predicate)
            .Select(Map.PullRequest)
            .ToList();

        _logger.LogDebug("Found {Count} of {Total} open pull requests for {Owner}/{Repo}.",
            pullRequests.Count, results.Count, input.Owner, input.Repo);

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return pullRequests;
#pragma warning restore CS8619
    }

    /// <summary>Adds the services to the associated collection.</summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    public async Task<StatusResponse> AddServicesAsync(PullRequestAddServicesInput input)
    {
        var serviceList = string.Join(' ', input.Services);
        _logger.LogInformation("Adding services {Services} to pull request {PullNumber} in {Owner}/{Repo}.",
            input.Services, input.PullNumber, input.Owner, input.Repo);
        return await AddCommentCommandAsync(input.Owner, input.Repo, input.PullNumber, $"/add {serviceList}");
    }

    /// <summary>Adds the comment command to the associated collection.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <param name="command">Text used as the command.</param>
    /// <returns>The status response produced by the operation.</returns>
    public async Task<StatusResponse> AddCommentCommandAsync(string owner, string repo, int pullNumber,
        string command)
    {
        // Command arguments come from GraphQL input, so only a single command line of simple words is posted.
        if (!CommandRegex().IsMatch(command))
        {
            throw new ValidationException([new ValidationFailure(nameof(command), "Invalid pull request command.")]);
        }

        await _gitHubSecurity.GuardRepoAsync(owner, repo);
        _logger.LogInformation("Posting command {Command} to pull request {PullNumber} in {Owner}/{Repo}.",
            command, pullNumber, owner, repo);
        var comment = $@"
{command}
";
        await _client.Issue.Comment.Create(
            owner,
            repo,
            pullNumber,
            comment
        );

        return new StatusResponse();
    }

    /// <summary>Matches a single slash command followed by word or hyphenated arguments and <c>--</c> flags.</summary>
    /// <returns>The compiled expression.</returns>
    [GeneratedRegex(@"^/[a-z]+(?: (?:--)?[\w-]+)*$")]
    private static partial Regex CommandRegex();
}

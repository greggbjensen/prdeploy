using LinqKit;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using Octokit;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.PullRequests.Inputs;
using PullRequest = PrDeploy.Api.Models.PullRequests.PullRequest;

namespace PrDeploy.Api.Business.Services;

/// <summary>
/// Coordinates pull request operations and their collaborators.
/// </summary>
public class PullRequestService : IPullRequestService
{
    private readonly IGitHubClient _client;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="client">Object containing the client data used by the operation.</param>
    public PullRequestService(IGitHubClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Finds and returns the open pull requests.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>Items matching the supplied criteria.</returns>
    public async Task<List<PullRequest>> ListOpenPullRequestsAsync(OpenPullRequestInput input)
    {
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

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return pullRequests;
#pragma warning restore CS8619
    }

    /// <summary>
    /// Adds the services to the associated collection.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The status response produced by the operation.</returns>
    public async Task<StatusResponse> AddServicesAsync(PullRequestAddServicesInput input)
    {
        var serviceList = string.Join(' ', input.Services);
        return await AddCommentCommandAsync(input.Owner, input.Repo, input.PullNumber, $"/add {serviceList}");
    }

    /// <summary>
    /// Adds the comment command to the associated collection.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="pullNumber">Number identifying the pull request.</param>
    /// <param name="command">Text used as the command.</param>
    /// <returns>The status response produced by the operation.</returns>
    public async Task<StatusResponse> AddCommentCommandAsync(string owner, string repo, int pullNumber,
        string command)
    {
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
}

using LinqKit;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models;
using Octokit;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Models.General;
using PullRequest = PrDeploy.Api.Models.PullRequests.PullRequest;

namespace PrDeploy.Api.Business.Services;

public class PullRequestService : IPullRequestService
{
    private readonly IGitHubClient _client;

    public PullRequestService(IGitHubClient client)
    {
        _client = client;
    }

    public async Task<List<PullRequest>> ListOpenPullRequestsAsync(string owner, string repo, string? search)
    {
        // Cannot search with this API, so we do it locally.
        var results = await _client.PullRequest.GetAllForRepository(
            owner,
            repo,
            new PullRequestRequest { State = ItemStateFilter.Open },
            new ApiOptions { PageCount = 1, PageSize = 100 });

        var predicate = PredicateBuilder.New<Octokit.PullRequest>(true);
        if (!string.IsNullOrWhiteSpace(search))
        {
            predicate.And(p => p.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                               || p.Number.ToString().Contains(search)
                               || (p.User.Name != null && p.User.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                               || p.User.Login.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var pullRequests = results.Where(predicate)
            .Select(Map.PullRequest)
            .ToList();

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return pullRequests;
#pragma warning restore CS8619
    }

    public async Task<StatusResponse> AddServicesAsync(string owner, string repo, int pullRequestNumber, List<string> services)
    {
        var serviceList = string.Join(' ', services);
        return await AddCommentCommandAsync(owner, repo, pullRequestNumber, $"/add {serviceList}");
    }

    public async Task<StatusResponse> AddCommentCommandAsync(string owner, string repo, int pullRequestNumber,
        string command)
    {
        var comment = $@"
{command}
";
        await _client.Issue.Comment.Create(
            owner,
            repo,
            pullRequestNumber,
            comment
        );

        return new StatusResponse();
    }
}

using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Business.Services;
public class OwnerRepoService : IOwnerRepoService
{
    private const string OwnerReposKey = "OWNER_REPOS";
    private readonly IParameterStore _parameterStore;
    private readonly IGitHubSecurity _gitHubSecurity;

    public OwnerRepoService(IParameterStore parameterStore, IGitHubSecurity gitHubSecurity)
    {
        _parameterStore = parameterStore;
        _gitHubSecurity = gitHubSecurity;
    }

    public async Task<StatusResponse> AddEnabledAsync(Repository repository)
    {
        await _gitHubSecurity.GuardRepoAsync(repository.Owner, repository.Repo);

        var ownerRepos = await ListEnabledAsync();
        var ownerRepo = ownerRepos.FirstOrDefault(o => 
            string.Equals(o.Owner, repository.Owner, StringComparison.OrdinalIgnoreCase));
        if (ownerRepo != null)
        {
            if (!ownerRepo.Repos.Contains(repository.Repo, StringComparer.OrdinalIgnoreCase))
            {
                ownerRepo.Repos.Add(repository.Repo.ToLower());
            }
        }
        else
        {
            ownerRepo = new OwnerRepos
            {
                Owner = repository.Owner.ToLower(),
                Repos = new List<string> { repository.Repo.ToLower() }
            };
            ownerRepos.Add(ownerRepo);
        }

        await _parameterStore.SetAsync(OwnerReposKey, ownerRepos);

        return new StatusResponse
        {
            Success = true
        };
    }

    public async Task<StatusResponse> RemoveEnabledAsync(Repository repository)
    {
        await _gitHubSecurity.GuardRepoAsync(repository.Owner, repository.Repo);

        var ownerRepos = await ListEnabledAsync();
        var ownerRepo = ownerRepos.FirstOrDefault(o =>
            string.Equals(o.Owner, repository.Owner, StringComparison.OrdinalIgnoreCase));
        if (ownerRepo != null)
        {
            ownerRepo.Repos.Remove(repository.Repo.ToLower());
            if (ownerRepo.Repos.Count == 0)
            {
                ownerRepos.Remove(ownerRepo);
            }
            await _parameterStore.SetAsync(OwnerReposKey, ownerRepos);
        }

        return new StatusResponse
        {
            Success = true
        };
    }

    public async Task<List<OwnerRepos>> ListEnabledAsync()
    {
        // TODO GBJ: Use _githubAppClient.GitHubApps.GetAllInstallationsForCurrent() once issue is fixed:
        // https://github.com/octokit/octokit.net/issues/2833
        var ownerRepos = await _parameterStore.GetAsync<List<OwnerRepos>?>(OwnerReposKey);
        if (ownerRepos == null)
        {
            ownerRepos = new List<OwnerRepos>();
            await _parameterStore.SetAsync(OwnerReposKey, ownerRepos);
        }

        return ownerRepos;
    }
}

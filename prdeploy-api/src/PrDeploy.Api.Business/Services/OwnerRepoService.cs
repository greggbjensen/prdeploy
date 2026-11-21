using Microsoft.Extensions.Logging;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Business.Services;
/// <summary>Coordinates owner repo operations and their collaborators.</summary>
public class OwnerRepoService : IOwnerRepoService
{
    private const string OwnerReposKey = "OWNER_REPOS";
    private readonly IDeploymentStore _deploymentStore;
    private readonly IGitHubSecurity _gitHubSecurity;
    private readonly ILogger<OwnerRepoService> _logger;

    /// <summary>Initializes the instance with its required dependencies.</summary>
    /// <param name="deploymentStore">Object containing the deployment store data used by the operation.</param>
    /// <param name="gitHubSecurity">Object containing the git hub security data used by the operation.</param>
    /// <param name="logger">Logger used to report operational activity.</param>
    public OwnerRepoService(IDeploymentStore deploymentStore, IGitHubSecurity gitHubSecurity, ILogger<OwnerRepoService> logger)
    {
        _deploymentStore = deploymentStore;
        _gitHubSecurity = gitHubSecurity;
        _logger = logger;
    }

    /// <summary>Enables a repository for prdeploy when the user is a contributor on it within an allowed organization.</summary>
    /// <param name="repository">Repository to enable.</param>
    /// <returns>A successful response once saved, or an unsuccessful response with a user facing message when
    /// the owner is not allowed for the user or the user is not a contributor; nothing is saved in that case.</returns>
    public async Task<StatusResponse> AddEnabledAsync(Repository repository)
    {
        // Reject with a message instead of a Forbidden error, so the portal keeps the user signed in
        // and shows the reason in the add dialog.
        if (!await _gitHubSecurity.HasOwnerAccessAsync(repository.Owner))
        {
            _logger.LogInformation("Rejected enabling {Owner}/{Repo}: owner is not an allowed organization of the user.",
                repository.Owner, repository.Repo);
            return new StatusResponse
            {
                Success = false,
                Message = $"{repository.Owner} is not an allowed organization, or you are not a member of it."
            };
        }

        if (!await _gitHubSecurity.HasRepoAccessAsync(repository.Owner, repository.Repo))
        {
            _logger.LogInformation("Rejected enabling {Owner}/{Repo}: user is not a contributor.",
                repository.Owner, repository.Repo);
            return new StatusResponse
            {
                Success = false,
                Message = $"You must be a contributor on {repository.Owner}/{repository.Repo} to add it."
            };
        }

        // Work on the full stored list; the user filtered list would drop repositories enabled by others.
        var ownerRepos = await LoadAllEnabledAsync();
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

        await _deploymentStore.SetAsync(OwnerReposKey, ownerRepos);
        _logger.LogInformation("Enabled repository {Owner}/{Repo}.", repository.Owner, repository.Repo);

        return new StatusResponse
        {
            Success = true
        };
    }

    /// <summary>Removes the enabled from the associated collection.</summary>
    /// <param name="repository">Repository whose values are addressed.</param>
    /// <returns>The status response produced by the operation.</returns>
    public async Task<StatusResponse> RemoveEnabledAsync(Repository repository)
    {
        await _gitHubSecurity.GuardRepoAsync(repository.Owner, repository.Repo);

        // Work on the full stored list; the user filtered list would drop repositories enabled by others.
        var ownerRepos = await LoadAllEnabledAsync();
        var ownerRepo = ownerRepos.FirstOrDefault(o =>
            string.Equals(o.Owner, repository.Owner, StringComparison.OrdinalIgnoreCase));
        if (ownerRepo != null)
        {
            ownerRepo.Repos.RemoveAll(r => string.Equals(r, repository.Repo, StringComparison.OrdinalIgnoreCase));
            if (ownerRepo.Repos.Count == 0)
            {
                ownerRepos.Remove(ownerRepo);
            }
            await _deploymentStore.SetAsync(OwnerReposKey, ownerRepos);
            _logger.LogInformation("Disabled repository {Owner}/{Repo}.", repository.Owner, repository.Repo);
        }
        else
        {
            _logger.LogDebug("Repository {Owner}/{Repo} was not enabled, nothing to remove.",
                repository.Owner, repository.Repo);
        }

        return new StatusResponse
        {
            Success = true
        };
    }

    /// <summary>Lists the enabled repositories, grouped by owner, that the signed in user has access to.</summary>
    /// <remarks>
    /// Repositories the user cannot access are left out. An empty list means either nothing is enabled yet or
    /// the user can access none of the enabled repositories; in both cases the portal offers to enable one.
    /// </remarks>
    /// <returns>Enabled owners and repositories accessible to the user; empty when there are none.</returns>
    public async Task<List<OwnerRepos>> ListEnabledAsync()
    {
        var ownerRepos = await LoadAllEnabledAsync();

        // If there are no repositories configured yet, then allow them to be set up.
        if (ownerRepos.Count == 0)
        {
            return ownerRepos;
        }

        // Filter out all repositories without access. When none are left the caller receives an empty list,
        // which lets them enable a repository they do have access to (guarded by AddEnabledAsync) instead of
        // being locked out by repositories other users enabled.
        var ownerReposList = new List<OwnerRepos>();
        foreach (var ownerRepo in ownerRepos)
        {
            var authorizedOwnerRepos = new OwnerRepos
            {
                Owner = ownerRepo.Owner,
                Repos = new List<string>()
            };
            foreach (var repo in ownerRepo.Repos)
            {
                if (await _gitHubSecurity.HasRepoAccessAsync(ownerRepo.Owner, repo))
                {
                    authorizedOwnerRepos.Repos.Add(repo);
                }
            }

            if (authorizedOwnerRepos.Repos.Count > 0)
            {
                ownerReposList.Add(authorizedOwnerRepos);
            }
        }

        if (ownerReposList.Count == 0)
        {
            _logger.LogInformation("User has access to none of the {Count} enabled owners.", ownerRepos.Count);
            return ownerReposList;
        }

        _logger.LogDebug("Listed {Count} enabled owners with accessible repositories.", ownerReposList.Count);
        return ownerReposList;
    }

    /// <summary>Loads every enabled repository from the store, regardless of the signed in user's access, creating and saving an empty list the first time.</summary>
    /// <remarks>
    /// Never return this list to a caller directly; use <see cref="ListEnabledAsync"/>, which filters by access.
    /// </remarks>
    /// <returns>All stored enabled owners and repositories.</returns>
    private async Task<List<OwnerRepos>> LoadAllEnabledAsync()
    {
        // TODO GBJ: Use _githubAppClient.GitHubApps.GetAllInstallationsForCurrent() once issue is fixed:
        // https://github.com/octokit/octokit.net/issues/2833
        var ownerRepos = await _deploymentStore.GetAsync<List<OwnerRepos>?>(OwnerReposKey);
        if (ownerRepos == null)
        {
            _logger.LogInformation("No enabled repositories stored yet, creating an empty list.");
            ownerRepos = new List<OwnerRepos>();
            await _deploymentStore.SetAsync(OwnerReposKey, ownerRepos);
        }

        return ownerRepos;
    }
}

using System.Net;
using Microsoft.Extensions.Logging;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Business.Services;
/// <summary>
/// Coordinates owner repo operations and their collaborators.
/// </summary>
public class OwnerRepoService : IOwnerRepoService
{
    private const string OwnerReposKey = "OWNER_REPOS";
    private readonly IDeploymentStore _deploymentStore;
    private readonly IGitHubSecurity _gitHubSecurity;
    private readonly ILogger<OwnerRepoService> _logger;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="deploymentStore">Object containing the deployment store data used by the operation.</param>
    /// <param name="gitHubSecurity">Object containing the git hub security data used by the operation.</param>
    /// <param name="logger">Logger used to report operational activity.</param>
    public OwnerRepoService(IDeploymentStore deploymentStore, IGitHubSecurity gitHubSecurity, ILogger<OwnerRepoService> logger)
    {
        _deploymentStore = deploymentStore;
        _gitHubSecurity = gitHubSecurity;
        _logger = logger;
    }

    /// <summary>
    /// Adds the enabled to the associated collection.
    /// </summary>
    /// <param name="repository">Repository whose values are addressed.</param>
    /// <returns>The status response produced by the operation.</returns>
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

        await _deploymentStore.SetAsync(OwnerReposKey, ownerRepos);
        _logger.LogInformation("Enabled repository {Owner}/{Repo}.", repository.Owner, repository.Repo);

        return new StatusResponse
        {
            Success = true
        };
    }

    /// <summary>
    /// Removes the enabled from the associated collection.
    /// </summary>
    /// <param name="repository">Repository whose values are addressed.</param>
    /// <returns>The status response produced by the operation.</returns>
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

    /// <summary>
    /// Finds and returns the enabled.
    /// </summary>
    /// <returns>Items matching the supplied criteria.</returns>
    public async Task<List<OwnerRepos>> ListEnabledAsync()
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


        // If there are no repositories configured yet, then allow them to be set up.
        if (ownerRepos.Count == 0)
        {
            return ownerRepos;
        }

        // Filter out all repositories without access,
        // if there are none left, than access is forbidden.
        var hasAuthorizedRepos = false;
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
                hasAuthorizedRepos = true;
            }
        }

        // If no repositories are authorized than access is denied.
        if (!hasAuthorizedRepos)
        {
            _logger.LogWarning("User has access to none of the {Count} enabled owners.", ownerRepos.Count);
            throw new HttpRequestException("Access denied.", null, HttpStatusCode.Forbidden);
        }
            
        _logger.LogDebug("Listed {Count} enabled owners with accessible repositories.", ownerReposList.Count);
        return ownerReposList;
    }
}

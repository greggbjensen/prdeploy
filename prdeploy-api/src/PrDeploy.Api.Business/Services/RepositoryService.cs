using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Services.Interfaces;
using Microsoft.Extensions.Options;
using Octokit;
using Repository = PrDeploy.Api.Models.Repositories.Repository;

namespace PrDeploy.Api.Business.Services;
public class RepositoryService : IRepositoryService
{
    private readonly IGitHubClient _client;
    private readonly PrDeployOptions _prDeployOptions;

    public RepositoryService(IOptions<PrDeployOptions> prDeployOptions, IGitHubClient client)
    {
        _client = client;
        _prDeployOptions = prDeployOptions.Value;
    }

    public Task<List<Repository>> ListPrDeployEnabledAsync()
    {
        // TODO GBJ: Use _githubAppClient.GitHubApps.GetAllInstallationsForCurrent() once issue is fixed:
        // https://github.com/octokit/octokit.net/issues/2833
        var enabledRepositories = _prDeployOptions.EnabledRepositories;
        return Task.FromResult(enabledRepositories)!;
    }
}

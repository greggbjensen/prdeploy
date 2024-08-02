using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Services.Interfaces;
using Microsoft.Extensions.Options;
using Repository = PrDeploy.Api.Models.Repository;

namespace PrDeploy.Api.Business.Services;
public class RepositoryService : IRepositoryService
{
    private readonly PrDeployOptions _prDeployOptions;

    public RepositoryService(IOptions<PrDeployOptions> prDeployOptions)
    {
        _prDeployOptions = prDeployOptions.Value;
    }

    public Task<List<Repository>> ListPrDeployEnabledAsync()
    {
        // TODO GBJ: Use _githubAppClient.GitHubApps.GetAllInstallationsForCurrent() once issue is fixed:
        // https://github.com/octokit/octokit.net/issues/2833
        return Task.FromResult(_prDeployOptions.EnabledRepositories)!;
    }
}

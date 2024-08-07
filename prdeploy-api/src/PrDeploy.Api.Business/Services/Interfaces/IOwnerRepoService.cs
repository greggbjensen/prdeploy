using PrDeploy.Api.Models;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IOwnerRepoService
{
    Task<List<OwnerRepos>> ListEnabledAsync();
    Task<StatusResponse> AddEnabledAsync(Repository repository);
    Task<StatusResponse> RemoveEnabledAsync(Repository repository);
}

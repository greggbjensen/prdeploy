using PrDeploy.Api.Models;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IRepositoryService
{
    Task<List<Repository>> ListPrDeployEnabledAsync();
}

using PrDeploy.Api.Models;
using PrDeploy.Api.Models.Repositories;

namespace PrDeploy.Api.Business.Services.Interfaces;

public interface IRepositoryService
{
    Task<List<Repository>> ListPrDeployEnabledAsync();
}

using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Models.General;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Schema.Mutations;

[ExtendObjectType("DeployMutation")]
public class OwnerRepoMutation
{
    [GraphQLName("ownerRepoAddEnabled")]
    public async Task<StatusResponse> AddEnabled(IOwnerRepoService service, Repository input) => 
        await service.AddEnabledAsync(input);

    [GraphQLName("ownerRepoRemoveEnabled")]
    public async Task<StatusResponse> RemoveEnabled(IOwnerRepoService service, Repository input) => 
        await service.RemoveEnabledAsync(input);
}

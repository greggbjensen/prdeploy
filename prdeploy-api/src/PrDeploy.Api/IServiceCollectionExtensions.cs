using Microsoft.AspNetCore.Authorization;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Filters;
using PrDeploy.Api.Mutations;
using PrDeploy.Api.Options;
using PrDeploy.Api.Queries;

namespace PrDeploy.Api;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPrDeployApi(this IServiceCollection services, DeployApiOptions options)
    {
        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType(q => q.Name("DeployQuery"))
            .AddType<DeployQueueQuery>()
            .AddType<DeployEnvironmentQuery>()
            .AddType<PullRequestQuery>()
            .AddType<RepositoryQuery>()
            .AddMutationType(m => m.Name("DeployMutation"))
            .AddType<DeployQueueMutation>()
            .AddType<DeployEnvironmentMutation>()
            .AddType<PullRequestMutation>()

            // Register service classes so they can be injected without a [Service] attribute.
            .RegisterService<IDeployQueueService>()
            .RegisterService<IDeployEnvironmentService>()
            .RegisterService<IPullRequestService>()
            .RegisterService<IRepositoryService>()
            .RegisterService<IRepoSettingsService>()

            // Filters.
            .AddErrorFilter<SanitizedErrorFilter>();

        services.AddScoped<IAuthorizationHandler, GitHubRepoAuthorizationHandler>();

        return services;
    }
}

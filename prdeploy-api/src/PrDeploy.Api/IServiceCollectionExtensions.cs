using Microsoft.AspNetCore.Authorization;
using Octokit.Internal;
using Octokit;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Filters;
using PrDeploy.Api.Mutations;
using PrDeploy.Api.Options;
using PrDeploy.Api.Queries;
using PrDeploy.Api.Business.Auth.Interfaces;

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
            .AddType<DeploySettingsQuery>()
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
            .RegisterService<IDeploySettingsService>()

            // Error handling.
            .AddErrorFilter<SanitizedErrorFilter>();

        services
            .AddHttpResponseFormatter<SanitizedHttpResponseFormatter>()
            .AddHttpContextAccessor()
            .AddScoped<ISecurityContext, SecurityContext>()
            .AddScoped<IGitHubClient>(s =>
            {
                var userToken = s.GetRequiredService<ISecurityContext>().UserToken;
                if (string.IsNullOrEmpty(userToken))
                {
                    throw new BadHttpRequestException("No token in security context.");
                }

                var credentials = new Credentials(userToken);
                return new GitHubClient(
                    new ProductHeaderValue("prdeploy"), new InMemoryCredentialStore(credentials));
            })
            .AddScoped<IAuthorizationHandler, GitHubRepoAuthorizationHandler>();

        return services;
    }
}

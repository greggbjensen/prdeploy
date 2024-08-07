using Microsoft.AspNetCore.Authorization;
using Octokit.Internal;
using Octokit;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Filters;
using PrDeploy.Api.Options;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Schema.Mutations;
using PrDeploy.Api.Schema.Queries;

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
            .AddType<OwnerRepoQuery>()
            .AddType<RepositoryQuery>()
            .AddMutationType(m => m.Name("DeployMutation"))
            .AddType<DeployEnvironmentMutation>()
            .AddType<DeployQueueMutation>()
            .AddType<OwnerRepoMutation>()
            .AddType<PullRequestMutation>()

            // Register service classes so they can be injected without a [Service] attribute.
            .RegisterService<IDeployQueueService>()
            .RegisterService<IDeployEnvironmentService>()
            .RegisterService<IPullRequestService>()
            .RegisterService<IOwnerRepoService>()
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

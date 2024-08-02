using Microsoft.AspNetCore.Authorization;
using Octokit.Internal;
using Octokit;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Filters;
using PrDeploy.Api.Mutations;
using PrDeploy.Api.Options;
using PrDeploy.Api.Queries;
using System.Security.Claims;

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

        services
            .AddHttpContextAccessor()
            .AddScoped<IGitHubClient>(s =>
            {
                var httpContext = s.GetRequiredService<IHttpContextAccessor>().HttpContext;
                if (httpContext == null)
                {
                    throw new BadHttpRequestException("No http context.");
                }

                var tokenClaim = httpContext.User.FindFirst("Token");
                if (tokenClaim == null)
                {
                    throw new BadHttpRequestException("No token in user context.");
                }

                var credentials = new Credentials(tokenClaim.Value);
                return new GitHubClient(
                    new ProductHeaderValue("prdeploy"), new InMemoryCredentialStore(credentials));
            })
            .AddScoped<IAuthorizationHandler, GitHubRepoAuthorizationHandler>();

        return services;
    }
}

using Microsoft.AspNetCore.Authorization;

namespace PrDeploy.Api.Auth
{
    public class GitHubRepoAuthorizationHandler : AuthorizationHandler<GitHubRepoAccessRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GitHubRepoAccessRequirement requirement)
        {
            // TODO: Implement auth handling.
            context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}

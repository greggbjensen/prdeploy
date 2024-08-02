using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace PrDeploy.Api.Auth
{
    public class GitHubRepoAuthorizationHandler : AuthorizationHandler<GitHubRepoAccessRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GitHubRepoAccessRequirement requirement)
        {
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, "User not authenticated."));
            }

            return Task.CompletedTask;
        }
    }
}

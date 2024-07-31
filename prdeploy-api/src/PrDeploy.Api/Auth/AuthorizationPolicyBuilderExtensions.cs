using Microsoft.AspNetCore.Authorization;

namespace PrDeploy.Api.Auth
{
    public static class AuthorizationPolicyBuilderExtensions
    {
        public static AuthorizationPolicyBuilder RequireGitHubRepoAccess(this AuthorizationPolicyBuilder builder)
        {
            builder.Requirements.Add(new GitHubRepoAccessRequirement());

            return builder;
        }
    }
}

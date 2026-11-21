using Microsoft.AspNetCore.Authorization;

namespace PrDeploy.Api.Auth
{
    /// <summary>Represents authorization policy builder extensions data used by the application.</summary>
    public static class AuthorizationPolicyBuilderExtensions
    {
        /// <summary>Performs the require git hub repo access operation and returns its result.</summary>
        /// <param name="builder">Object containing the builder data used by the operation.</param>
        /// <returns>The authorization policy builder produced by the operation.</returns>
        public static AuthorizationPolicyBuilder RequireGitHubRepoAccess(this AuthorizationPolicyBuilder builder)
        {
            builder.Requirements.Add(new GitHubRepoAccessRequirement());

            return builder;
        }
    }
}

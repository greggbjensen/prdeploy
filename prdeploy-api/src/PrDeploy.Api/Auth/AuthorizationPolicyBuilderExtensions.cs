using Microsoft.AspNetCore.Authorization;

namespace PrDeploy.Api.Auth
{
    /// <summary>
    /// Represents the authorization Policy Builder Extensions used by the application.
    /// </summary>
    public static class AuthorizationPolicyBuilderExtensions
    {
        /// <summary>
        /// Performs the require Git Hub Repo Access operation and returns its result.
        /// </summary>
        /// <param name="builder">Value supplied for builder.</param>
        /// <returns>The value produced by this operation.</returns>
        public static AuthorizationPolicyBuilder RequireGitHubRepoAccess(this AuthorizationPolicyBuilder builder)
        {
            builder.Requirements.Add(new GitHubRepoAccessRequirement());

            return builder;
        }
    }
}

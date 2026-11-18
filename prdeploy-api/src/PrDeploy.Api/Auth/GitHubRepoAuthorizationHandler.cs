using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace PrDeploy.Api.Auth
{
    /// <summary>
    /// Represents git hub repo authorization handler data used by the application.
    /// </summary>
    public class GitHubRepoAuthorizationHandler : AuthorizationHandler<GitHubRepoAccessRequirement>
    {
        private readonly ILogger<GitHubRepoAuthorizationHandler> _logger;

        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        /// <param name="logger">Logger used to report authorization decisions.</param>
        public GitHubRepoAuthorizationHandler(ILogger<GitHubRepoAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handles the requirement according to the application's workflow.
        /// </summary>
        /// <param name="context">Request or application context used by the operation.</param>
        /// <param name="requirement">Object containing the requirement data used by the operation.</param>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GitHubRepoAccessRequirement requirement)
        {
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation("Authorization failed: user is not authenticated.");
                context.Fail(new AuthorizationFailureReason(this, "User not authenticated."));
            }

            return Task.CompletedTask;
        }
    }
}

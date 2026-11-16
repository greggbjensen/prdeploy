using Microsoft.AspNetCore.Authorization;

namespace PrDeploy.Api.Auth
{
    /// <summary>
    /// Represents git hub repo access requirement data used by the application.
    /// </summary>
    public class GitHubRepoAccessRequirement : IAuthorizationRequirement
    {
    }
}

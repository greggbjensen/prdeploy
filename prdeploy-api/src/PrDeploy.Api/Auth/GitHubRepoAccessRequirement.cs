using Microsoft.AspNetCore.Authorization;

namespace PrDeploy.Api.Auth
{
    /// <summary>
    /// Represents the git Hub Repo Access Requirement used by the application.
    /// </summary>
    public class GitHubRepoAccessRequirement : IAuthorizationRequirement
    {
    }
}

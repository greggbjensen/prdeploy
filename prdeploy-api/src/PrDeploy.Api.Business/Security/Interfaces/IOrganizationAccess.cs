using Octokit;

namespace PrDeploy.Api.Business.Security.Interfaces;

/// <summary>Decides whether a GitHub user belongs to one of the organizations prdeploy is configured to allow.</summary>
public interface IOrganizationAccess
{
    /// <summary>Determines whether the owner is in the configured allowed organizations list.</summary>
    /// <param name="owner">Organization or account login that owns a repository.</param>
    /// <returns>True when the owner is allowed; otherwise, false.</returns>
    bool IsAllowedOrganization(string owner);

    /// <summary>Determines whether the user is allowed for the owner: the owner is in the allowed list and is either the user's own account or an organization where the user's membership is active.</summary>
    /// <param name="client">GitHub client authenticated as the user.</param>
    /// <param name="login">GitHub login of the user.</param>
    /// <param name="owner">Organization or account login to check.</param>
    /// <returns>True when the user is an allowed member of the owner; otherwise, false.</returns>
    Task<bool> IsMemberAsync(IGitHubClient client, string login, string owner);

    /// <summary>Determines whether the user is an allowed member of at least one configured organization.</summary>
    /// <param name="client">GitHub client authenticated as the user.</param>
    /// <param name="login">GitHub login of the user.</param>
    /// <returns>True when the user belongs to any allowed organization; otherwise, false.</returns>
    Task<bool> IsMemberOfAnyAllowedAsync(IGitHubClient client, string login);
}

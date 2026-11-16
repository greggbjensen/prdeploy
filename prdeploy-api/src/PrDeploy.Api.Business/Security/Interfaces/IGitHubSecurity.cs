namespace PrDeploy.Api.Business.Security.Interfaces;

/// <summary>
/// Defines the contract for git Hub Security operations.
/// </summary>
public interface IGitHubSecurity
{
    /// <summary>
    /// Enforces access requirements for the Repo.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    Task GuardRepoAsync(string owner, string repo);
    /// <summary>
    /// Enforces access requirements for the Owner.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    Task GuardOwnerAsync(string owner);
    /// <summary>
    /// Determines whether the repo access is available.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <returns>True when the requested condition is met; otherwise, false.</returns>
    Task<bool> HasRepoAccessAsync(string owner, string repo);
    /// <summary>
    /// Determines whether the owner access is available.
    /// </summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <returns>True when the requested condition is met; otherwise, false.</returns>
    Task<bool> HasOwnerAccessAsync(string owner);
}
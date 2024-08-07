namespace PrDeploy.Api.Business.Security.Interfaces;

public interface IGitHubSecurity
{
    Task GuardRepoAsync(string owner, string repo);
    Task GuardOwnerAsync(string owner);
    Task<bool> HasRepoAccessAsync(string owner, string repo);
    Task<bool> HasOwnerAccessAsync(string owner);
}
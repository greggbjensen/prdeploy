using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.OwnerRepo;

namespace PrDeploy.Api.Tests.Security;

/// <summary>
/// Verifies how <see cref="OwnerRepoService"/> filters and guards enabled repositories per user.
/// </summary>
public class OwnerRepoServiceSecurityTest
{
    private const string OwnerReposKey = "OWNER_REPOS";
    private readonly Mock<IDeploymentStore> _store = new();
    private readonly Mock<IGitHubSecurity> _security = new();

    /// <summary>
    /// Builds the service under test over the supplied stored enabled repositories.
    /// </summary>
    /// <param name="enabled">Enabled owners and repositories held in the store.</param>
    /// <returns>Service wired to the mocked store and security.</returns>
    private OwnerRepoService CreateService(List<OwnerRepos> enabled)
    {
        _store.Setup(s => s.GetAsync<List<OwnerRepos>?>(OwnerReposKey)).ReturnsAsync(enabled);
        return new OwnerRepoService(_store.Object, _security.Object, NullLogger<OwnerRepoService>.Instance);
    }

    /// <summary>
    /// Confirms a user who can access none of the repositories enabled by others gets an empty list
    /// rather than a Forbidden error, so the portal can offer to enable their own repository.
    /// </summary>
    [Fact]
    public async Task ListEnabledAsync_OnlyOtherUsersRepos_ReturnsEmpty()
    {
        var service = CreateService(new List<OwnerRepos>
        {
            new() { Owner = "greggbjensen", Repos = new List<string> { "prdeploy-example-repo" } }
        });

        Assert.Empty(await service.ListEnabledAsync());
    }

    /// <summary>
    /// Confirms only repositories the user can access are returned, and owners left empty are dropped.
    /// </summary>
    [Fact]
    public async Task ListEnabledAsync_MixedAccess_ReturnsOnlyAccessibleRepos()
    {
        _security.Setup(s => s.HasRepoAccessAsync("journosa", "journosa")).ReturnsAsync(true);
        var service = CreateService(new List<OwnerRepos>
        {
            new() { Owner = "greggbjensen", Repos = new List<string> { "prdeploy-example-repo" } },
            new() { Owner = "journosa", Repos = new List<string> { "journosa", "private" } }
        });

        var result = await service.ListEnabledAsync();

        var ownerRepos = Assert.Single(result);
        Assert.Equal("journosa", ownerRepos.Owner);
        Assert.Equal(new[] { "journosa" }, ownerRepos.Repos);
    }

    /// <summary>
    /// Confirms a user can enable a repository they have access to even when other repositories are enabled.
    /// </summary>
    [Fact]
    public async Task AddEnabledAsync_AccessibleRepo_AddsAlongsideExisting()
    {
        _security.Setup(s => s.HasOwnerAccessAsync("journosa")).ReturnsAsync(true);
        _security.Setup(s => s.HasRepoAccessAsync("journosa", "journosa")).ReturnsAsync(true);
        var service = CreateService(new List<OwnerRepos>
        {
            new() { Owner = "greggbjensen", Repos = new List<string> { "prdeploy-example-repo" } }
        });

        var response = await service.AddEnabledAsync(
            new Repository { Owner = "journosa", Repo = "journosa" });

        Assert.True(response.Success);
        _store.Verify(s => s.SetAsync(OwnerReposKey, It.Is<List<OwnerRepos>>(l =>
            l.Count == 2 && l.Any(o => o.Owner == "journosa" && o.Repos.Contains("journosa"))), false));
    }

    /// <summary>
    /// Confirms disabling a repository keeps repositories enabled by other users that the caller cannot access.
    /// </summary>
    [Fact]
    public async Task RemoveEnabledAsync_KeepsOtherUsersRepos()
    {
        var service = CreateService(new List<OwnerRepos>
        {
            new() { Owner = "greggbjensen", Repos = new List<string> { "prdeploy-example-repo" } },
            new() { Owner = "journosa", Repos = new List<string> { "journosa" } }
        });

        await service.RemoveEnabledAsync(new Repository { Owner = "Journosa", Repo = "Journosa" });

        _store.Verify(s => s.SetAsync(OwnerReposKey, It.Is<List<OwnerRepos>>(l =>
            l.Count == 1 && l[0].Owner == "greggbjensen"), false));
    }

    /// <summary>
    /// Confirms adding a repository the user is not a contributor on is rejected with a message, not an error,
    /// so the portal keeps the user signed in, and nothing is saved.
    /// </summary>
    [Fact]
    public async Task AddEnabledAsync_NotContributor_ReturnsRejectionWithoutSaving()
    {
        _security.Setup(s => s.HasOwnerAccessAsync("journosa")).ReturnsAsync(true);
        _security.Setup(s => s.HasRepoAccessAsync("journosa", "readonly")).ReturnsAsync(false);
        var service = CreateService(new List<OwnerRepos>());

        var response = await service.AddEnabledAsync(new Repository { Owner = "journosa", Repo = "readonly" });

        Assert.False(response.Success);
        Assert.Equal("You must be a contributor on journosa/readonly to add it.", response.Message);
        _store.Verify(s => s.SetAsync(OwnerReposKey, It.IsAny<List<OwnerRepos>>(), It.IsAny<bool>()), Times.Never);
    }

    /// <summary>
    /// Confirms adding a repository whose owner is not an allowed organization of the user is rejected
    /// with a message before repository access is checked.
    /// </summary>
    [Fact]
    public async Task AddEnabledAsync_OwnerNotAllowed_ReturnsRejectionWithoutSaving()
    {
        var service = CreateService(new List<OwnerRepos>());

        var response = await service.AddEnabledAsync(new Repository { Owner = "other-org", Repo = "r" });

        Assert.False(response.Success);
        Assert.Equal("other-org is not an allowed organization, or you are not a member of it.", response.Message);
        _security.Verify(s => s.HasRepoAccessAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _store.Verify(s => s.SetAsync(OwnerReposKey, It.IsAny<List<OwnerRepos>>(), It.IsAny<bool>()), Times.Never);
    }
}

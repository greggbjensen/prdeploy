using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Security;

namespace PrDeploy.Api.Tests.Security;

/// <summary>
/// Verifies how <see cref="GitHubSecurity"/> decides repository and owner access from GitHub responses.
/// </summary>
public class GitHubSecurityTest : IDisposable
{
    private const string Token = "gh-token";
    private const string Login = "octocat";
    private readonly Mock<IGitHubClient> _github = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    /// <summary>
    /// Makes every user an active member of any organization by default; tests narrow this where needed.
    /// </summary>
    public GitHubSecurityTest()
    {
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Membership(MembershipState.Active));
    }

    /// <summary>
    /// Releases the in-memory cache used to store access decisions.
    /// </summary>
    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Builds the security service under test for the supplied caller.
    /// </summary>
    /// <param name="token">GitHub token of the caller; empty simulates an anonymous request.</param>
    /// <param name="login">GitHub login of the caller.</param>
    /// <returns>Security service wired to the mocked GitHub client and shared cache.</returns>
    private GitHubSecurity CreateSecurity(string token = Token, string login = Login)
    {
        var context = new Mock<ISecurityContext>();
        context.SetupGet(c => c.UserToken).Returns(token);
        context.SetupGet(c => c.Login).Returns(login);
        var organizationAccess = new OrganizationAccess(
            Microsoft.Extensions.Options.Options.Create(new GitHubAuthOptions
            {
                AllowedOrganizations = new List<string> { "journosa", "o", "octocat" }
            }),
            NullLogger<OrganizationAccess>.Instance);
        return new GitHubSecurity(_github.Object, NullLogger<GitHubSecurity>.Instance, _cache, context.Object,
            organizationAccess);
    }

    /// <summary>
    /// Creates an Octokit repository whose permissions block reflects the authenticated user.
    /// </summary>
    /// <param name="permissions">Permissions GitHub reports for the caller, or null when omitted.</param>
    /// <returns>Repository model carrying the supplied permissions.</returns>
    private static Octokit.Repository RepositoryWith(RepositoryPermissions? permissions)
    {
        var repository = new Octokit.Repository();
        typeof(Octokit.Repository).GetProperty(nameof(Octokit.Repository.Permissions))!
            .SetValue(repository, permissions);
        return repository;
    }

    /// <summary>
    /// Creates Octokit repository permissions with only the named levels granted.
    /// </summary>
    /// <param name="admin">Whether admin permission is granted.</param>
    /// <param name="maintain">Whether maintain permission is granted.</param>
    /// <param name="push">Whether push (write) permission is granted.</param>
    /// <param name="triage">Whether triage permission is granted.</param>
    /// <param name="pull">Whether pull (read) permission is granted.</param>
    /// <returns>Permissions model with the requested flags.</returns>
    private static RepositoryPermissions Permissions(bool admin = false, bool maintain = false, bool push = false,
        bool triage = false, bool pull = false) => new(admin, maintain, push, triage, pull);

    /// <summary>
    /// Creates an Octokit organization membership in the supplied state.
    /// </summary>
    /// <param name="state">Membership state reported by GitHub.</param>
    /// <returns>Organization membership model with the requested state.</returns>
    private static OrganizationMembership Membership(MembershipState state)
    {
        var membership = new OrganizationMembership();
        typeof(OrganizationMembership).GetProperty(nameof(OrganizationMembership.State))!
            .SetValue(membership, new StringEnum<MembershipState>(state));
        return membership;
    }

    /// <summary>
    /// Confirms any write level permission (as held by outside collaborators and team members) grants access.
    /// </summary>
    /// <param name="admin">Whether admin permission is granted.</param>
    /// <param name="maintain">Whether maintain permission is granted.</param>
    /// <param name="push">Whether push permission is granted.</param>
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    public async Task HasRepoAccessAsync_WritePermission_ReturnsTrue(bool admin, bool maintain, bool push)
    {
        _github.Setup(g => g.Repository.Get("journosa", "journosa"))
            .ReturnsAsync(RepositoryWith(Permissions(admin, maintain, push, pull: true)));

        Assert.True(await CreateSecurity().HasRepoAccessAsync("journosa", "journosa"));
    }

    /// <summary>
    /// Confirms read and triage users, and public repository visitors, cannot manage deployments.
    /// </summary>
    /// <param name="triage">Whether triage permission is granted alongside read.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HasRepoAccessAsync_ReadOnlyPermission_ReturnsFalse(bool triage)
    {
        _github.Setup(g => g.Repository.Get("o", "r"))
            .ReturnsAsync(RepositoryWith(Permissions(triage: triage, pull: true)));

        Assert.False(await CreateSecurity().HasRepoAccessAsync("o", "r"));
    }

    /// <summary>
    /// Confirms a repository response without a permissions block is treated as no access.
    /// </summary>
    [Fact]
    public async Task HasRepoAccessAsync_PermissionsMissing_ReturnsFalse()
    {
        _github.Setup(g => g.Repository.Get("o", "r")).ReturnsAsync(RepositoryWith(null));

        Assert.False(await CreateSecurity().HasRepoAccessAsync("o", "r"));
    }

    /// <summary>
    /// Confirms a repository hidden from the token (GitHub 404) is denied rather than thrown.
    /// </summary>
    [Fact]
    public async Task HasRepoAccessAsync_RepositoryNotVisible_ReturnsFalse()
    {
        _github.Setup(g => g.Repository.Get("o", "r"))
            .ThrowsAsync(new NotFoundException("Not Found", HttpStatusCode.NotFound));

        Assert.False(await CreateSecurity().HasRepoAccessAsync("o", "r"));
    }

    /// <summary>
    /// Confirms a granted decision is cached per token, case-insensitively, while denials are re-checked.
    /// </summary>
    [Fact]
    public async Task HasRepoAccessAsync_Granted_IsCachedCaseInsensitively()
    {
        _github.Setup(g => g.Repository.Get(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(RepositoryWith(Permissions(push: true, pull: true)));
        var security = CreateSecurity();

        Assert.True(await security.HasRepoAccessAsync("Journosa", "Journosa"));
        Assert.True(await security.HasRepoAccessAsync("journosa", "journosa"));

        _github.Verify(g => g.Repository.Get(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// Confirms a cached grant for one token is not reused for a different token.
    /// </summary>
    [Fact]
    public async Task HasRepoAccessAsync_DifferentToken_DoesNotReuseCache()
    {
        _github.SetupSequence(g => g.Repository.Get("o", "r"))
            .ReturnsAsync(RepositoryWith(Permissions(push: true, pull: true)))
            .ReturnsAsync(RepositoryWith(Permissions(pull: true)));

        Assert.True(await CreateSecurity("token-a").HasRepoAccessAsync("o", "r"));
        Assert.False(await CreateSecurity("token-b", "someone-else").HasRepoAccessAsync("o", "r"));
    }

    /// <summary>
    /// Confirms the guard throws Forbidden when the user lacks write access.
    /// </summary>
    [Fact]
    public async Task GuardRepoAsync_NoWriteAccess_ThrowsForbidden()
    {
        _github.Setup(g => g.Repository.Get("o", "r")).ReturnsAsync(RepositoryWith(Permissions(pull: true)));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => CreateSecurity().GuardRepoAsync("o", "r"));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    /// <summary>
    /// Confirms a contributor is denied when the repository owner is not in the allowed organizations,
    /// without asking GitHub about the repository.
    /// </summary>
    [Fact]
    public async Task HasRepoAccessAsync_OwnerNotAllowed_ReturnsFalse()
    {
        _github.Setup(g => g.Repository.Get("other-org", "r"))
            .ReturnsAsync(RepositoryWith(Permissions(admin: true, push: true, pull: true)));

        Assert.False(await CreateSecurity().HasRepoAccessAsync("other-org", "r"));

        _github.Verify(g => g.Repository.Get(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Confirms an outside collaborator with push access is denied when they are not a member of the
    /// allowed organization that owns the repository.
    /// </summary>
    [Fact]
    public async Task HasRepoAccessAsync_ContributorButNotOrgMember_ReturnsFalse()
    {
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("journosa", Login))
            .ThrowsAsync(new NotFoundException("Not Found", HttpStatusCode.NotFound));
        _github.Setup(g => g.Repository.Get("journosa", "journosa"))
            .ReturnsAsync(RepositoryWith(Permissions(push: true, pull: true)));

        Assert.False(await CreateSecurity().HasRepoAccessAsync("journosa", "journosa"));
    }

    /// <summary>
    /// Confirms an owner outside the allowed organizations is denied even for an active member.
    /// </summary>
    [Fact]
    public async Task HasOwnerAccessAsync_OwnerNotAllowed_ReturnsFalseWithoutLookup()
    {
        Assert.False(await CreateSecurity().HasOwnerAccessAsync("other-org"));

        _github.Verify(g => g.Organization.Member.GetOrganizationMembership(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    /// Confirms an active organization member has owner access.
    /// </summary>
    [Fact]
    public async Task HasOwnerAccessAsync_ActiveMember_ReturnsTrue()
    {
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("journosa", Login))
            .ReturnsAsync(Membership(MembershipState.Active));

        Assert.True(await CreateSecurity().HasOwnerAccessAsync("journosa"));
    }

    /// <summary>
    /// Confirms a pending invitation does not grant owner access.
    /// </summary>
    [Fact]
    public async Task HasOwnerAccessAsync_PendingMember_ReturnsFalse()
    {
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("o", Login))
            .ReturnsAsync(Membership(MembershipState.Pending));

        Assert.False(await CreateSecurity().HasOwnerAccessAsync("o"));
    }

    /// <summary>
    /// Confirms an outside collaborator (not an organization member) is denied owner access.
    /// </summary>
    [Fact]
    public async Task HasOwnerAccessAsync_NotMember_ReturnsFalse()
    {
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("o", Login))
            .ThrowsAsync(new NotFoundException("Not Found", HttpStatusCode.NotFound));

        Assert.False(await CreateSecurity().HasOwnerAccessAsync("o"));
    }

    /// <summary>
    /// Confirms a user owns their personal account without calling the organization API.
    /// </summary>
    [Fact]
    public async Task HasOwnerAccessAsync_OwnAccount_ReturnsTrueWithoutMembershipLookup()
    {
        Assert.True(await CreateSecurity().HasOwnerAccessAsync("OctoCat"));

        _github.Verify(g => g.Organization.Member.GetOrganizationMembership(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    /// Confirms anonymous callers are denied for both repository and owner checks.
    /// </summary>
    [Fact]
    public async Task NoToken_DeniesRepoAndOwner()
    {
        var security = CreateSecurity(string.Empty, string.Empty);

        Assert.False(await security.HasRepoAccessAsync("o", "r"));
        Assert.False(await security.HasOwnerAccessAsync("o"));
    }

    /// <summary>
    /// Confirms names that are not valid on GitHub, including path segments that could change storage keys or API
    /// paths, are denied without calling GitHub.
    /// </summary>
    /// <param name="owner">Owner name.</param>
    /// <param name="repo">Repository name.</param>
    [Theory]
    [InlineData("o", "r/../other")]
    [InlineData("o", "..")]
    [InlineData("o", ".")]
    [InlineData("o", "r%2F..")]
    [InlineData("o", "")]
    [InlineData("o/x", "r")]
    [InlineData("-o", "r")]
    [InlineData("", "r")]
    public async Task HasRepoAccessAsync_InvalidName_ReturnsFalseWithoutLookup(string owner, string repo)
    {
        var security = CreateSecurity();

        Assert.False(await security.HasRepoAccessAsync(owner, repo));
        _github.Verify(g => g.Repository.Get(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _github.Verify(g => g.Organization.Member.GetOrganizationMembership(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    /// Confirms valid GitHub names, including dots, underscores, and hyphens, pass name validation.
    /// </summary>
    /// <param name="owner">Owner name.</param>
    /// <param name="repo">Repository name.</param>
    [Theory]
    [InlineData("my-org", "my_repo.js")]
    [InlineData("o", ".github")]
    public void NameValidation_AcceptsValidNames(string owner, string repo)
    {
        Assert.True(GitHubSecurity.IsValidOwnerName(owner));
        Assert.True(GitHubSecurity.IsValidRepoName(repo));
    }
}

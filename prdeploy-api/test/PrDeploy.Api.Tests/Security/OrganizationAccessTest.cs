using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Security;

namespace PrDeploy.Api.Tests.Security;

/// <summary>Verifies <see cref="OrganizationAccess"/> against the configured allowed organizations.</summary>
public class OrganizationAccessTest
{
    private const string Login = "gregg-journosa";
    private readonly Mock<IGitHubClient> _github = new();

    /// <summary>Builds the service under test with the supplied allowed organizations.</summary>
    /// <param name="allowed">Configured allowed organizations.</param>
    /// <returns>Organization access over the supplied list.</returns>
    private static OrganizationAccess Create(params string[] allowed) => new(
        Microsoft.Extensions.Options.Options.Create(new GitHubAuthOptions { AllowedOrganizations = allowed.ToList() }),
        NullLogger<OrganizationAccess>.Instance);

    /// <summary>Creates an Octokit organization membership in the supplied state.</summary>
    /// <param name="state">Membership state reported by GitHub.</param>
    /// <returns>Organization membership model with the requested state.</returns>
    private static OrganizationMembership Membership(MembershipState state)
    {
        var membership = new OrganizationMembership();
        typeof(OrganizationMembership).GetProperty(nameof(OrganizationMembership.State))!
            .SetValue(membership, new StringEnum<MembershipState>(state));
        return membership;
    }

    /// <summary>Confirms allowed organization matching ignores case and surrounding whitespace, and rejects blanks.</summary>
    [Fact]
    public void IsAllowedOrganization_MatchesCaseInsensitively()
    {
        var access = Create(" Journosa ", "");

        Assert.True(access.IsAllowedOrganization("journosa"));
        Assert.False(access.IsAllowedOrganization("other"));
        Assert.False(access.IsAllowedOrganization(""));
    }

    /// <summary>Confirms an active member of an allowed organization can sign in.</summary>
    [Fact]
    public async Task IsMemberOfAnyAllowedAsync_ActiveMember_ReturnsTrue()
    {
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("journosa", Login))
            .ReturnsAsync(Membership(MembershipState.Active));

        Assert.True(await Create("journosa").IsMemberOfAnyAllowedAsync(_github.Object, Login));
    }

    /// <summary>Confirms membership in any one of several allowed organizations is enough.</summary>
    [Fact]
    public async Task IsMemberOfAnyAllowedAsync_MemberOfSecond_ReturnsTrue()
    {
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("first", Login))
            .ThrowsAsync(new NotFoundException("Not Found", HttpStatusCode.NotFound));
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("second", Login))
            .ReturnsAsync(Membership(MembershipState.Active));

        Assert.True(await Create("first", "second").IsMemberOfAnyAllowedAsync(_github.Object, Login));
    }

    /// <summary>Confirms non-members, pending invitations, and GitHub failures are all denied.</summary>
    [Fact]
    public async Task IsMemberOfAnyAllowedAsync_NotActiveAnywhere_ReturnsFalse()
    {
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("missing", Login))
            .ThrowsAsync(new NotFoundException("Not Found", HttpStatusCode.NotFound));
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("pending", Login))
            .ReturnsAsync(Membership(MembershipState.Pending));
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("broken", Login))
            .ThrowsAsync(new ApiException("Restricted", HttpStatusCode.Forbidden));

        Assert.False(await Create("missing", "pending", "broken").IsMemberOfAnyAllowedAsync(_github.Object, Login));
    }

    /// <summary>Confirms nobody can sign in when no organizations are configured.</summary>
    [Fact]
    public async Task IsMemberOfAnyAllowedAsync_NoneConfigured_ReturnsFalseWithoutLookup()
    {
        Assert.False(await Create().IsMemberOfAnyAllowedAsync(_github.Object, Login));

        _github.Verify(g => g.Organization.Member.GetOrganizationMembership(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>Confirms a listed personal account is only allowed for that user.</summary>
    [Fact]
    public async Task IsMemberAsync_ListedPersonalAccount_AllowsOnlyThatUser()
    {
        _github.Setup(g => g.Organization.Member.GetOrganizationMembership("greggbjensen", "someone-else"))
            .ThrowsAsync(new NotFoundException("Not Found", HttpStatusCode.NotFound));
        var access = Create("greggbjensen");

        Assert.True(await access.IsMemberAsync(_github.Object, "GreggBJensen", "greggbjensen"));
        Assert.False(await access.IsMemberAsync(_github.Object, "someone-else", "greggbjensen"));
    }

    /// <summary>Confirms an owner outside the list is denied even for its own user.</summary>
    [Fact]
    public async Task IsMemberAsync_OwnerNotListed_ReturnsFalse()
    {
        Assert.False(await Create("journosa").IsMemberAsync(_github.Object, Login, Login));
    }
}

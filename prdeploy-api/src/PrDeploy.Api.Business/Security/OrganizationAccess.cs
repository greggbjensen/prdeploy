using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Security.Interfaces;

namespace PrDeploy.Api.Business.Security;

/// <summary>Checks GitHub organization membership against the <see cref="GitHubAuthOptions.AllowedOrganizations"/> list.</summary>
/// <remarks>
/// Results are not cached here; callers that check repeatedly, such as <see cref="GitHubSecurity"/>, cache them.
/// </remarks>
public class OrganizationAccess : IOrganizationAccess
{
    private readonly HashSet<string> _allowedOrganizations;
    private readonly ILogger<OrganizationAccess> _logger;

    /// <summary>Initializes the instance with the configured allowed organizations.</summary>
    /// <param name="options">GitHub authentication options holding the allowed organizations.</param>
    /// <param name="logger">Logger used to report membership decisions.</param>
    public OrganizationAccess(IOptions<GitHubAuthOptions> options, ILogger<OrganizationAccess> logger)
    {
        _logger = logger;
        _allowedOrganizations = new HashSet<string>(
            (options.Value.AllowedOrganizations ?? new List<string>())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim()),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool IsAllowedOrganization(string owner)
    {
        return !string.IsNullOrWhiteSpace(owner) && _allowedOrganizations.Contains(owner);
    }

    /// <inheritdoc />
    public async Task<bool> IsMemberAsync(IGitHubClient client, string login, string owner)
    {
        if (string.IsNullOrEmpty(login))
        {
            return false;
        }

        if (!IsAllowedOrganization(owner))
        {
            _logger.LogInformation("Owner {Owner} is not in the allowed organizations.", owner);
            return false;
        }

        // A listed personal account is only available to that user.
        if (string.Equals(owner, login, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            var membership = await client.Organization.Member.GetOrganizationMembership(owner, login);
            var isActive = membership?.State.Value == MembershipState.Active;
            if (!isActive)
            {
                _logger.LogInformation("{Login} membership in organization {Owner} is not active.", login, owner);
            }

            return isActive;
        }
        catch (NotFoundException)
        {
            _logger.LogInformation("{Login} is not a member of organization {Owner}.", login, owner);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to verify {Login} membership in organization {Owner}.", login, owner);
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> IsMemberOfAnyAllowedAsync(IGitHubClient client, string login)
    {
        if (_allowedOrganizations.Count == 0)
        {
            _logger.LogWarning("No allowed organizations are configured in GitHubAuth:AllowedOrganizations; denying {Login}.",
                login);
            return false;
        }

        foreach (var organization in _allowedOrganizations)
        {
            if (await IsMemberAsync(client, login, organization))
            {
                return true;
            }
        }

        _logger.LogInformation("{Login} is not a member of any allowed organization.", login);
        return false;
    }
}

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Security.Interfaces;

namespace PrDeploy.Api.Business.Security
{
    /// <summary>Represents git hub security data used by the application.</summary>
    public partial class GitHubSecurity : IGitHubSecurity
    {
        private static readonly TimeSpan AccessRefreshTimeSpan = TimeSpan.FromMinutes(10);
        private readonly IGitHubClient _client;
        private readonly ILogger<GitHubSecurity> _logger;
        private readonly IMemoryCache _cache;
        private readonly ISecurityContext _securityContext;
        private readonly IOrganizationAccess _organizationAccess;

        /// <summary>Initializes the instance with its required dependencies.</summary>
        /// <param name="client">GitHub client authenticated as the signed in user.</param>
        /// <param name="logger">Logger used to report operational information and errors.</param>
        /// <param name="cache">Cache holding granted access decisions per token.</param>
        /// <param name="securityContext">Signed in user's login and GitHub token.</param>
        /// <param name="organizationAccess">Checks membership in the configured allowed organizations.</param>
        public GitHubSecurity(IGitHubClient client, ILogger<GitHubSecurity> logger, IMemoryCache cache,
            ISecurityContext securityContext, IOrganizationAccess organizationAccess)
        {
            _organizationAccess = organizationAccess;
            _cache = cache;
            _securityContext = securityContext;
            _client = client;
            _logger = logger;
        }

        /// <summary>Enforces access requirements for the Repo.</summary>
        /// <param name="owner">Repository owner or organization whose values are addressed.</param>
        /// <param name="repo">Repository whose values are addressed.</param>
        public async Task GuardRepoAsync(string owner, string repo)
        {
            var hasAccess = await HasRepoAccessAsync(owner, repo);
            if (!hasAccess)
            {
                _logger.LogWarning("Access denied for {Login} to repository {Owner}/{Repo}.",
                    _securityContext.Login, owner, repo);
                throw new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden);
            }
        }

        /// <summary>Enforces access requirements for the Owner.</summary>
        /// <param name="owner">Repository owner or organization whose values are addressed.</param>
        public async Task GuardOwnerAsync(string owner)
        {
            var hasAccess = await HasOwnerAccessAsync(owner);
            if (!hasAccess)
            {
                _logger.LogWarning("Access denied for {Login} to owner {Owner}.", _securityContext.Login, owner);
                throw new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden);
            }
        }

        /// <summary>Determines whether the signed in user is a contributor (can push) on a repository owned by an allowed organization they belong to.</summary>
        /// <remarks>
        /// The owner must first pass <see cref="HasOwnerAccessAsync"/>. Then the code reads the repository with the user's own token and inspects the <c>permissions</c> block GitHub returns
        /// for the authenticated user. This reflects every way the user can hold access (direct collaborator,
        /// outside collaborator, team membership, organization base permission, or owner) and only needs the
        /// repository to be visible to the token. Push (write) access is required so that read-only users, and
        /// everyone on a public repository, cannot change deployments or settings. Positive results are cached
        /// per token for <see cref="AccessRefreshTimeSpan"/>.
        /// </remarks>
        /// <param name="owner">Repository owner or organization whose values are addressed.</param>
        /// <param name="repo">Repository whose values are addressed.</param>
        /// <returns>True when the owner is allowed for the user and the user has push, maintain, or admin
        /// permission on the repository; otherwise, false.</returns>
        public async Task<bool> HasRepoAccessAsync(string owner, string repo)
        {
            if (!IsValidRepoName(repo))
            {
                _logger.LogWarning("Rejected access check for invalid repository name {Repo}.", repo);
                return false;
            }

            var userToken = _securityContext.UserToken;
            if (string.IsNullOrEmpty(userToken))
            {
                _logger.LogDebug("No user token present while checking access to {Owner}/{Repo}.", owner, repo);
                return false;
            }

            var userHash = HashToken(userToken);
            var authKey = $"auth/{owner}/{repo}/{userHash}".ToLowerInvariant();
            if (_cache.TryGetValue(authKey, out _))
            {
                _logger.LogDebug("Repository access for {Login} to {Owner}/{Repo} served from cache.",
                    _securityContext.Login, owner, repo);
                return true;
            }

            if (!await HasOwnerAccessAsync(owner))
            {
                return false;
            }

            var hasAccess = false;
            try
            {
                var repository = await _client.Repository.Get(owner, repo);
                hasAccess = HasWriteAccess(repository?.Permissions);
                if (hasAccess)
                {
                    _cache.Set(authKey, true, AccessRefreshTimeSpan);
                    _logger.LogDebug("Verified repository access for {Login} to {Owner}/{Repo}.",
                        _securityContext.Login, owner, repo);
                }
                else
                {
                    _logger.LogInformation("{Login} does not have push access to repository {Owner}/{Repo}.",
                        _securityContext.Login, owner, repo);
                }
            }
            catch (NotFoundException)
            {
                // GitHub returns 404 when the token cannot see the repository at all.
                _logger.LogInformation("Repository {Owner}/{Repo} is not visible to {Login}.",
                    owner, repo, _securityContext.Login);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to verify {Login} access to repository {Owner}/{Repo}.",
                    _securityContext.Login, owner, repo);
            }

            return hasAccess;
        }

        /// <summary>Determines whether the owner is in the configured allowed organizations and the signed in user belongs to it.</summary>
        /// <remarks>
        /// Delegates to <see cref="IOrganizationAccess.IsMemberAsync"/>: the owner must be listed in
        /// <c>GitHubAuth:AllowedOrganizations</c> and be either the user's own account or an organization where the
        /// user's membership is active. Granted decisions are cached per token for <see cref="AccessRefreshTimeSpan"/>.
        /// </remarks>
        /// <param name="owner">Repository owner or organization whose values are addressed.</param>
        /// <returns>True when the owner is allowed and the user belongs to it; otherwise, false.</returns>
        public async Task<bool> HasOwnerAccessAsync(string owner)
        {
            if (!IsValidOwnerName(owner))
            {
                _logger.LogWarning("Rejected access check for invalid owner name {Owner}.", owner);
                return false;
            }

            var userToken = _securityContext.UserToken;
            if (string.IsNullOrEmpty(userToken))
            {
                _logger.LogDebug("No user token present while checking access to owner {Owner}.", owner);
                return false;
            }

            var userHash = HashToken(userToken);
            var authKey = $"auth/{owner}/{userHash}".ToLowerInvariant();
            if (_cache.TryGetValue(authKey, out _))
            {
                _logger.LogDebug("Owner access for {Login} to {Owner} served from cache.",
                    _securityContext.Login, owner);
                return true;
            }

            var hasAccess = await _organizationAccess.IsMemberAsync(_client, _securityContext.Login, owner);

            if (hasAccess)
            {
                _cache.Set(authKey, true, AccessRefreshTimeSpan);
                _logger.LogDebug("Verified owner access for {Login} to {Owner}.", _securityContext.Login, owner);
            }

            return hasAccess;
        }

        /// <summary>Checks that an owner is a valid GitHub user or organization name.</summary>
        /// <remarks>
        /// Owner and repository names are used in storage keys and GitHub API paths, so characters such as <c>/</c>
        /// or <c>..</c> must be rejected before an access check could pass for a different resource.
        /// </remarks>
        /// <param name="owner">Owner name to check.</param>
        /// <returns><c>true</c> when the name only contains characters GitHub allows.</returns>
        public static bool IsValidOwnerName(string? owner) => owner != null && OwnerNameRegex().IsMatch(owner);

        /// <summary>Checks that a repository name is valid on GitHub and is not a relative path segment.</summary>
        /// <param name="repo">Repository name to check.</param>
        /// <returns><c>true</c> when the name only contains characters GitHub allows.</returns>
        public static bool IsValidRepoName(string? repo) =>
            repo != null && repo != "." && repo != ".." && RepoNameRegex().IsMatch(repo);

        /// <summary>Matches GitHub owner names: alphanumerics and single hyphens, up to 39 characters.</summary>
        /// <returns>The compiled expression.</returns>
        [GeneratedRegex("^[A-Za-z0-9](?:[A-Za-z0-9-]{0,38})$")]
        private static partial Regex OwnerNameRegex();

        /// <summary>Matches GitHub repository names: alphanumerics, '.', '_' and '-', up to 100 characters.</summary>
        /// <returns>The compiled expression.</returns>
        [GeneratedRegex("^[A-Za-z0-9._-]{1,100}$")]
        private static partial Regex RepoNameRegex();

        /// <summary>Determines whether GitHub reports write level permission for the authenticated user.</summary>
        /// <param name="permissions">Permissions GitHub returned for the authenticated user, if any.</param>
        /// <returns>True when push, maintain, or admin permission is present; otherwise, false.</returns>
        private static bool HasWriteAccess(RepositoryPermissions? permissions)
        {
            return permissions != null && (permissions.Push || permissions.Maintain || permissions.Admin);
        }

        /// <summary>Produces a stable, non reversible cache key fragment for the user's token so that cached access decisions are scoped to a single token and the raw token is never stored as a key.</summary>
        /// <param name="userToken">GitHub token of the signed in user.</param>
        /// <returns>Uppercase hexadecimal SHA-256 hash of the token.</returns>
        private static string HashToken(string userToken)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(userToken));
            return Convert.ToHexString(hashBytes);
        }
    }
}

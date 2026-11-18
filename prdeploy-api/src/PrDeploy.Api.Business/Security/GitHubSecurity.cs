using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Security.Interfaces;

namespace PrDeploy.Api.Business.Security
{
    /// <summary>
    /// Represents git hub security data used by the application.
    /// </summary>
    public class GitHubSecurity : IGitHubSecurity
    {
        private static readonly TimeSpan AccessRefreshTimeSpan = TimeSpan.FromMinutes(10);
        private readonly IGitHubClient _client;
        private readonly ILogger<GitHubSecurity> _logger;
        private readonly IMemoryCache _cache;
        private readonly ISecurityContext _securityContext;

        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        /// <param name="client">Object containing the client data used by the operation.</param>
        /// <param name="logger">Logger used to report operational information and errors.</param>
        /// <param name="cache">Object containing the cache data used by the operation.</param>
        /// <param name="securityContext">Object containing the security context data used by the operation.</param>
        public GitHubSecurity(IGitHubClient client, ILogger<GitHubSecurity> logger, IMemoryCache cache, ISecurityContext securityContext)
        {
            _cache = cache;
            _securityContext = securityContext;
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Enforces access requirements for the Repo.
        /// </summary>
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

        /// <summary>
        /// Enforces access requirements for the Owner.
        /// </summary>
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

        /// <summary>
        /// Determines whether the repo access is available.
        /// </summary>
        /// <param name="owner">Repository owner or organization whose values are addressed.</param>
        /// <param name="repo">Repository whose values are addressed.</param>
        /// <returns>True when the requested condition is met; otherwise, false.</returns>
        public async Task<bool> HasRepoAccessAsync(string owner, string repo)
        {
            var userToken = _securityContext.UserToken;
            if (string.IsNullOrEmpty(userToken))
            {
                _logger.LogDebug("No user token present while checking access to {Owner}/{Repo}.", owner, repo);
                return false;
            }

            var userHash = HashToken(userToken);
            var authKey = $"auth/{owner}/{repo}/{userHash}";
            var hasAccess = _cache.TryGetValue(authKey, out _);
            if (hasAccess)
            {
                _logger.LogDebug("Repository access for {Login} to {Owner}/{Repo} served from cache.",
                    _securityContext.Login, owner, repo);
                return true;
            }

            try
            {
                hasAccess = await _client.Repository.Collaborator.IsCollaborator(owner, repo, _securityContext.Login);
                if (hasAccess)
                {
                    _cache.Set(authKey, hasAccess, AccessRefreshTimeSpan);
                    _logger.LogDebug("Verified repository access for {Login} to {Owner}/{Repo}.",
                        _securityContext.Login, owner, repo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to verify {Login} access to repository {Owner}/{Repo}.",
                    _securityContext.Login, owner, repo);
            }

            return hasAccess;
        }

        /// <summary>
        /// Determines whether the owner access is available.
        /// </summary>
        /// <param name="owner">Repository owner or organization whose values are addressed.</param>
        /// <returns>True when the requested condition is met; otherwise, false.</returns>
        public async Task<bool> HasOwnerAccessAsync(string owner)
        {
            var userToken = _securityContext.UserToken;
            if (string.IsNullOrEmpty(userToken))
            {
                _logger.LogDebug("No user token present while checking access to owner {Owner}.", owner);
                return false;
            }

            var userHash = HashToken(userToken);
            var authKey = $"auth/{owner}/{userHash}";
            var hasAccess = _cache.TryGetValue(authKey, out _);
            if (hasAccess)
            {
                _logger.LogDebug("Owner access for {Login} to {Owner} served from cache.",
                    _securityContext.Login, owner);
                return true;
            }

            try
            {
                var membership =
                    await _client.Organization.Member.GetOrganizationMembership(owner, _securityContext.Login);
                hasAccess = membership.State.Value == MembershipState.Active;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Login} is not an organization member of {Owner}, trying user account.",
                    _securityContext.Login, owner);
            }

            if (!hasAccess)
            {
                try
                {
                    hasAccess = string.Equals(owner, _securityContext.Login, StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to compare {Login} to user account {Owner}.", _securityContext.Login, owner);
                }
            }

            if (hasAccess)
            {
                _cache.Set(authKey, true, AccessRefreshTimeSpan);
                _logger.LogDebug("Verified owner access for {Login} to {Owner}.", _securityContext.Login, owner);
            }

            return hasAccess;
        }

        /// <summary>
        /// Determines whether the hash token is available.
        /// </summary>
        /// <param name="userToken">Text used as the user token.</param>
        /// <returns>Text produced or selected by the operation.</returns>
        private static string HashToken(string userToken)
        {
            var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.Default.GetBytes(userToken));
            var hash = ByteArrayToString(hashBytes);
            return hash;
        }

        //// SourceRef: https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/compute-hash-values
        /// <summary>
        /// Performs the byte array to string operation and returns its result.
        /// </summary>
        /// <param name="arrInput">Object containing the arr input data used by the operation.</param>
        /// <returns>Text produced or selected by the operation.</returns>
        private static string ByteArrayToString(byte[] arrInput)
        {
            int i;
            StringBuilder sOutput = new StringBuilder(arrInput.Length);
            for (i = 0; i < arrInput.Length; i++)
            {
                sOutput.Append(arrInput[i].ToString("X2"));
            }
            return sOutput.ToString();
        }

        /// <summary>
        /// Throws an exception when access Denied is encountered.
        /// </summary>
        private static void ThrowAccessDenied()
        {
            throw new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden);
        }
    }
}

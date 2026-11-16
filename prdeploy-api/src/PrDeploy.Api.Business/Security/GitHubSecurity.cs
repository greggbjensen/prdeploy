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
                return false;
            }

            var userHash = HashToken(userToken);
            var authKey = $"auth/{owner}/{repo}/{userHash}";
            var hasAccess = _cache.TryGetValue(authKey, out _);
            if (hasAccess)
            {
                return true;
            }

            try
            {
                hasAccess = await _client.Repository.Collaborator.IsCollaborator(owner, repo, _securityContext.Login);
                if (hasAccess)
                {
                    _cache.Set(authKey, hasAccess, AccessRefreshTimeSpan);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"User did not have access to {owner}/{repo}");
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
                return false;
            }

            var userHash = HashToken(userToken);
            var authKey = $"auth/{owner}/{userHash}";
            var hasAccess = _cache.TryGetValue(authKey, out _);
            if (hasAccess)
            {
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
                _logger.LogWarning($"User did not have access to {owner}, trying user account.\n{ex.Message}");
            }

            if (!hasAccess)
            {
                try
                {
                    hasAccess = string.Equals(owner, _securityContext.Login, StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"User did not have access {owner} user account.\n {ex.Message}");
                }
            }

            if (hasAccess)
            {
                _cache.Set(authKey, true, AccessRefreshTimeSpan);
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

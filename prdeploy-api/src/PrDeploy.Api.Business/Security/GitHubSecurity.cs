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
    public class GitHubSecurity : IGitHubSecurity
    {
        private static readonly TimeSpan AccessRefreshTimeSpan = TimeSpan.FromMinutes(10);
        private readonly IGitHubClient _client;
        private readonly ILogger<GitHubSecurity> _logger;
        private readonly IMemoryCache _cache;
        private readonly ISecurityContext _securityContext;

        public GitHubSecurity(IGitHubClient client, ILogger<GitHubSecurity> logger, IMemoryCache cache, ISecurityContext securityContext)
        {
            _cache = cache;
            _securityContext = securityContext;
            _client = client;
            _logger = logger;
        }

        public async Task GuardRepoAsync(string owner, string repo)
        {
            var hasAccess = await HasRepoAccessAsync(owner, repo);
            if (!hasAccess)
            {
                throw new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden);
            }
        }

        public async Task GuardOwnerAsync(string owner)
        {
            var hasAccess = await HasOwnerAccessAsync(owner);
            if (!hasAccess)
            {
                throw new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden);
            }
        }

        public async Task<bool> HasRepoAccessAsync(string owner, string repo)
        {
            var userToken = _securityContext.UserToken;
            if (string.IsNullOrEmpty(userToken))
            {
                return false;
            }

            var userHash = HashToken(userToken);
            var authKey = $"auth/{owner}/{repo}/{userHash}";
            var hasAccess = _cache.TryGetValue(authKey, out object _);
            if (hasAccess)
            {
                return true;
            }

            try
            {
                var repository = await _client.Repository.Get(owner, repo);
                hasAccess = repository != null;
                if (hasAccess)
                {
                    _cache.Set(authKey, true, AccessRefreshTimeSpan);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"User did not have access to {owner}/{repo}");
            }

            return hasAccess;
        }

        public async Task<bool> HasOwnerAccessAsync(string owner)
        {
            var userToken = _securityContext.UserToken;
            if (string.IsNullOrEmpty(userToken))
            {
                return false;
            }

            var userHash = HashToken(userToken);
            var authKey = $"auth/{owner}/{userHash}";
            var hasAccess = _cache.TryGetValue(authKey, out object _);
            if (hasAccess)
            {
                return true;
            }

            try
            {
                var organization = await _client.Organization.Get(owner);
                hasAccess = organization != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"User did not have access to {owner}, trying user account.\n{ex.Message}");
            }

            if (!hasAccess)
            {
                try
                {
                    var user = await _client.User.Current();
                    hasAccess = string.Equals(owner, user.Login, StringComparison.OrdinalIgnoreCase);
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

        private static string HashToken(string userToken)
        {
            var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.Default.GetBytes(userToken));
            var hash = ByteArrayToString(hashBytes);
            return hash;
        }

        //// SourceRef: https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/compute-hash-values
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

        private static void ThrowAccessDenied()
        {
            throw new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden);
        }
    }
}

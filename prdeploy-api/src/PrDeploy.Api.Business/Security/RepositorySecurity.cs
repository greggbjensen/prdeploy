using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Security.Interfaces;

namespace PrDeploy.Api.Business.Security
{
    public class RepositorySecurity : IRepositorySecurity
    {
        private static readonly TimeSpan AccessRefreshTimeSpan = TimeSpan.FromMinutes(10);
        private readonly IGitHubClient _client;
        private readonly ILogger<RepositorySecurity> _logger;
        private readonly IMemoryCache _cache;
        private readonly ISecurityContext _securityContext;

        public RepositorySecurity(IGitHubClient client, ILogger<RepositorySecurity> logger, IMemoryCache cache, ISecurityContext securityContext)
        {
            _cache = cache;
            _securityContext = securityContext;
            _client = client;
            _logger = logger;
        }

        public async Task GuardAsync(string owner, string repo)
        {
            var hasAccess = await HasAccessAsync(owner, repo);
            if (!hasAccess)
            {
                throw new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden);
            }
        }

        public async Task<bool> HasAccessAsync(string owner, string repo)
        {
            var userToken = _securityContext.UserToken;
            if (string.IsNullOrEmpty(userToken))
            {
                return false;
            }

            var userHash = HashToken(userToken);
            var authKey = $"{owner}/{repo}/{userHash}";
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

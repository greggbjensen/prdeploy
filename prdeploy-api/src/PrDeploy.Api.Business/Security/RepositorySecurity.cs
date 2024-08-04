using System.Net;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using PrDeploy.Api.Business.Security.Interfaces;

namespace PrDeploy.Api.Business.Security
{
    public class RepositorySecurity : IRepositorySecurity
    {
        private static readonly TimeSpan AccessRefreshTimeSpan = TimeSpan.FromMinutes(10);
        private readonly IGitHubClient _client;
        private readonly ILogger<RepositorySecurity> _logger;
        private readonly IMemoryCache _cache;

        public RepositorySecurity(IGitHubClient client, ILogger<RepositorySecurity> logger, IMemoryCache cache)
        {
            _cache = cache;
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
            var tokenClaim = ClaimsPrincipal.Current?.FindFirst("Token");
            if (tokenClaim == null || string.IsNullOrEmpty(tokenClaim.Value))
            {
                return false;
            }

            var authKey = $"{owner}/{repo}/{tokenClaim.Value}";
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

        private static void ThrowAccessDenied()
        {
            throw new HttpRequestException("Access denied", null, HttpStatusCode.Forbidden);
        }
    }
}

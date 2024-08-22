using System.IdentityModel.Tokens.Jwt;
using PrDeploy.Api.Business.Auth;
using PrDeploy.Api.Business.Auth.Interfaces;

namespace PrDeploy.Api.Auth
{
    public class SecurityContext : ISecurityContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public SecurityContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string UserToken
        {
            get
            {
                if (_httpContextAccessor?.HttpContext?.User == null)
                {
                    return string.Empty;
                }

                var tokenClaim = _httpContextAccessor.HttpContext.User.FindFirst(PrDeployClaimNames.GitHubToken);
                return tokenClaim != null ? tokenClaim.Value : string.Empty;
            }
        }

        public string Login
        {
            get
            {
                if (_httpContextAccessor?.HttpContext?.User == null)
                {
                    return string.Empty;
                }

                var loginClaim = _httpContextAccessor.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Name);
                return loginClaim != null ? loginClaim.Value : string.Empty;
            }
        }
    }
}

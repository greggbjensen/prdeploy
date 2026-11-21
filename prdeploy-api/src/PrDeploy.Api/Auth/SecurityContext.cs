using System.IdentityModel.Tokens.Jwt;
using PrDeploy.Api.Business.Auth;
using PrDeploy.Api.Business.Auth.Interfaces;

namespace PrDeploy.Api.Auth
{
    /// <summary>Represents security context data used by the application.</summary>
    public class SecurityContext : ISecurityContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        /// <summary>Initializes the instance with its required dependencies.</summary>
        /// <param name="httpContextAccessor">Object containing the HTTP context accessor data used by the operation.</param>
        public SecurityContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>Gets the user token.</summary>
        /// <value>Current user token value.</value>
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

        /// <summary>Gets the login.</summary>
        /// <value>Current login value.</value>
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

using System.IdentityModel.Tokens.Jwt;
using PrDeploy.Api.Business.Auth;
using PrDeploy.Api.Business.Auth.Interfaces;

namespace PrDeploy.Api.Auth
{
    /// <summary>
    /// Represents the security Context used by the application.
    /// </summary>
    public class SecurityContext : ISecurityContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        /// <summary>
        /// Initializes a security Context with the supplied dependencies.
        /// </summary>
        /// <param name="httpContextAccessor">Value supplied for http Context Accessor.</param>
        public SecurityContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the User Token.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
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

        /// <summary>
        /// Gets the Login.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
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

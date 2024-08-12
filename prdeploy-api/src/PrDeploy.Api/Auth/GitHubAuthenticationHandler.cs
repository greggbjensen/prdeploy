using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using PrDeploy.Api.Business.Auth;
using PrDeploy.Api.Business.Auth.Interfaces;

namespace PrDeploy.Api.Auth
{
    public class GitHubAuthenticationHandler : JwtBearerHandler
    {
        private readonly ICipherService _cipherService;

        public GitHubAuthenticationHandler(
            IOptionsMonitor<JwtBearerOptions> options, 
            ILoggerFactory logger, 
            UrlEncoder encoder,
            ISystemClock clock, 
            ICipherService cipherService)
            : base(options, logger, encoder, clock)
        {
            _cipherService = cipherService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var result = await base.HandleAuthenticateAsync();
            if (!result.Succeeded)
            {
                return result;
            }

            var claim = result.Principal.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
            {
                return AuthenticateResult.Fail($"Required {JwtRegisteredClaimNames.Sub} not present.");
            }

            var encryptedToken = claim.Value;
            var gitHubToken = _cipherService.Decrypt(encryptedToken);
            var claims = new List<Claim>
            {
                // This claim is only present locally.
                new (PrDeployClaimNames.GitHubToken, gitHubToken)
            };
            var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, JwtBearerDefaults.AuthenticationScheme);
            Context.User = principal;
            Context.Items["User"] = principal;

            // We only need to verify the token is present because we pass it through for auth in GitHub.
            return AuthenticateResult.Success(ticket);
        }
    }
}

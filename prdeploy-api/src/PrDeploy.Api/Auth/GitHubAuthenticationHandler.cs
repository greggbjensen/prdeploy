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
    /// <summary>
    /// Represents git hub authentication handler data used by the application.
    /// </summary>
    public class GitHubAuthenticationHandler : JwtBearerHandler
    {
        private readonly ICipherService _cipherService;

        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        /// <param name="options">Object containing the options data used by the operation.</param>
        /// <param name="logger">Logger used to report operational information and errors.</param>
        /// <param name="encoder">Object containing the encoder data used by the operation.</param>
        /// <param name="cipherService">Object containing the cipher service data used by the operation.</param>
        public GitHubAuthenticationHandler(
            IOptionsMonitor<JwtBearerOptions> options, 
            ILoggerFactory logger, 
            UrlEncoder encoder,
            ICipherService cipherService)
            : base(options, logger, encoder)
        {
            _cipherService = cipherService;
        }

        /// <summary>
        /// Handles the authenticate according to the application's workflow.
        /// </summary>
        /// <returns>The authenticate result produced by the operation.</returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var result = await base.HandleAuthenticateAsync();
            if (!result.Succeeded)
            {
                return result;
            }

            var subClaim = result.Principal.FindFirst(JwtRegisteredClaimNames.Sub);
            if (subClaim == null)
            {
                Logger.LogWarning("Rejected token: required claim {Claim} not present.", JwtRegisteredClaimNames.Sub);
                return AuthenticateResult.Fail($"Required claim {JwtRegisteredClaimNames.Sub} not present.");
            }

            var nameClaim = result.Principal.FindFirst(JwtRegisteredClaimNames.Name);
            if (nameClaim == null)
            {
                Logger.LogWarning("Rejected token: required claim {Claim} not present.", JwtRegisteredClaimNames.Name);
                return AuthenticateResult.Fail($"Required claim {JwtRegisteredClaimNames.Name} not present.");
            }

            string gitHubToken;
            try
            {
                var encryptedToken = subClaim.Value;
                gitHubToken = _cipherService.Decrypt(encryptedToken);
            }
            catch (Exception ex)
            {
                // If we are unable to decrypt than it is unauthorized.
                Logger.LogWarning(ex, "Rejected token for {Login}: unable to decrypt claim {Claim}.",
                    nameClaim.Value, JwtRegisteredClaimNames.Sub);
                return AuthenticateResult.Fail($"Unable to process claim {JwtRegisteredClaimNames.Sub}.");
            }

            var claims = new List<Claim>
            {
                // This claim is only present locally. The decrypted token is not copied into sub, so code that
                // enumerates standard claims, such as logging enrichers, cannot expose it.
                new (PrDeployClaimNames.GitHubToken, gitHubToken),
                new (JwtRegisteredClaimNames.Name, nameClaim.Value)
            };
            var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, JwtBearerDefaults.AuthenticationScheme);
            Context.User = principal;
            Context.Items["User"] = principal;

            Logger.LogDebug("Authenticated {Login}.", nameClaim.Value);

            // We only need to verify the token is present because we pass it through for auth in GitHub.
            return AuthenticateResult.Success(ticket);
        }
    }
}

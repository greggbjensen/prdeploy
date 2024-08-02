using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace PrDeploy.Api.Auth
{
    public class GitHubAuthenticationHandler : AuthenticationHandler<GitHubAuthenticationSchemeOptions>
    {
        public GitHubAuthenticationHandler(
            IOptionsMonitor<GitHubAuthenticationSchemeOptions> options, 
            ILoggerFactory logger, 
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authorization = Request.Headers.Authorization;
            if (authorization.Count == 0 || string.IsNullOrWhiteSpace(authorization[0]))
            {
                return Task.FromResult(AuthenticateResult.Fail("Authorization header with Bearer must be provided."));
            }

            var value = authorization[0];
            if (!value!.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Authentication value must be Bearer."));
            }

            var parts = value.Split(' ');
            if (parts.Length == 1)
            {
                return Task.FromResult(AuthenticateResult.Fail("Authentication Bearer token is missing."));
            }

            var token = parts[1];
            var claims = new List<Claim>
            {
                new ("Token", token)
            };
            var identity = new ClaimsIdentity(claims, GitHubAuthenticationSchemeOptions.SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, GitHubAuthenticationSchemeOptions.SchemeName);
            Context.User = principal;
            Context.Items["User"] = principal;

            // We only need to verify the token is present because we pass it through for auth in GitHub.
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

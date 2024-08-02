using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Auth
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, Action<GitHubAuthOptions> configure)
        {
            var gitHubAuthOptions = new GitHubAuthOptions();
            configure(gitHubAuthOptions);

            if (string.IsNullOrWhiteSpace(gitHubAuthOptions.Authority))
            {
                throw new ArgumentException("GitHubAuth.Authority is required in options.");
            }

            if (string.IsNullOrWhiteSpace(gitHubAuthOptions.Audience))
            {
                throw new ArgumentException("GitHubAuth.Audience is required in options.");
            }

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = gitHubAuthOptions.Authority;
                    options.Audience = gitHubAuthOptions.Audience;
                    options.Configuration = new OpenIdConnectConfiguration();
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = ClaimTypes.NameIdentifier,
                        AuthenticationType = JwtBearerDefaults.AuthenticationScheme
                    };

                    // This should only be enabled for troubleshooting.
                    // IdentityModelEventSource.ShowPII = true;
                });

            return services;
        }
    }
}

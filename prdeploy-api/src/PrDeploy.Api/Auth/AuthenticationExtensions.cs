using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PrDeploy.Api.Options;

namespace PrDeploy.Api.Auth
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, Action<JwtOptions> configure)
        {
            var jwtOptions = new JwtOptions();
            configure(jwtOptions);

            if (string.IsNullOrWhiteSpace(jwtOptions.Authority))
            {
                throw new ArgumentException("Jwt.Authority is required in options.");
            }

            if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
            {
                throw new ArgumentException("Jwt.Audience is required in options.");
            }

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = jwtOptions.Authority;
                    options.Audience = jwtOptions.Audience;
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

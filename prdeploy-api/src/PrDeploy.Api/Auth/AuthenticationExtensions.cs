using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PrDeploy.Api.Business.Options;
using System.Text;

namespace PrDeploy.Api.Auth
{
    /// <summary>
    /// Represents authentication extensions data used by the application.
    /// </summary>
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Adds the git hub authentication to the associated collection.
        /// </summary>
        /// <param name="services">Services to include in the operation.</param>
        /// <param name="configureGitHub">Object containing the configure git hub data used by the operation.</param>
        /// <param name="configureJwt">Object containing the configure JWT data used by the operation.</param>
        /// <returns>The i service collection produced by the operation.</returns>
        public static IServiceCollection AddGitHubAuthentication(this IServiceCollection services, 
            Action<GitHubAuthOptions> configureGitHub, Action<JwtOptions> configureJwt)
        {
            var gitHubAuthOptions = new GitHubAuthOptions();
            configureGitHub(gitHubAuthOptions);

            var jwtOptions = new JwtOptions();
            configureJwt(jwtOptions);

            if (string.IsNullOrWhiteSpace(jwtOptions.Key))
            {
                throw new InvalidOperationException(
                    "JWT signing key is not configured. Set the 'Jwt:Key' configuration value.");
            }

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddScheme<JwtBearerOptions, GitHubAuthenticationHandler>(
                    JwtBearerDefaults.AuthenticationScheme, 
                    o =>
                    {
                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
                        o.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidIssuer = jwtOptions.Issuer,
                            ValidAudience = jwtOptions.Audience,
                            IssuerSigningKey = securityKey,
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true
                        };
                        o.MapInboundClaims = false;
                    });

            return services;
        }
    }
}

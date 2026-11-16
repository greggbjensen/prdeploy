using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PrDeploy.Api.Business.Options;
using System.Text;

namespace PrDeploy.Api.Auth
{
    /// <summary>
    /// Represents the authentication Extensions used by the application.
    /// </summary>
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Adds the Git Hub Authentication to the associated collection.
        /// </summary>
        /// <param name="services">Services to include in the operation.</param>
        /// <param name="configureGitHub">Value supplied for configure Git Hub.</param>
        /// <param name="configureJwt">Value supplied for configure Jwt.</param>
        /// <returns>The value produced by this operation.</returns>
        public static IServiceCollection AddGitHubAuthentication(this IServiceCollection services, 
            Action<GitHubAuthOptions> configureGitHub, Action<JwtOptions> configureJwt)
        {
            var gitHubAuthOptions = new GitHubAuthOptions();
            configureGitHub(gitHubAuthOptions);

            var jwtOptions = new JwtOptions();
            configureJwt(jwtOptions);

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

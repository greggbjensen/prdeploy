using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Auth
{
    public static class AuthenticationExtensions
    {
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
                        ValidateIssuerSigningKey = true,
                        NameClaimType = JwtRegisteredClaimNames.Sub
                    };
                });

            return services;
        }
    }
}

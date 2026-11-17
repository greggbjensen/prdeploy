using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Tests.Auth;

/// <summary>
/// Contains tests that verify authentication configuration.
/// </summary>
public class AuthenticationExtensionsTest
{
    /// <summary>
    /// Verifies that authentication setup rejects a missing JWT signing key.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void AddGitHubAuthentication_ThrowsWhenSigningKeyIsMissing(string signingKey)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddGitHubAuthentication(
                _ => { },
                options => options.Key = signingKey));
        Assert.Contains("Jwt:Key", exception.Message);
    }

    /// <summary>
    /// Verifies that authentication uses the configured JWT signing key.
    /// </summary>
    [Fact]
    public void AddGitHubAuthentication_UsesConfiguredSigningKey()
    {
        const string signingKey = "configured-signing-key";
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddGitHubAuthentication(
            _ => { },
            options => options.Key = signingKey);

        using var serviceProvider = services.BuildServiceProvider();
        var jwtOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal(
            Encoding.UTF8.GetBytes(signingKey),
            Assert.IsType<SymmetricSecurityKey>(jwtOptions.TokenValidationParameters.IssuerSigningKey).Key);
    }
}

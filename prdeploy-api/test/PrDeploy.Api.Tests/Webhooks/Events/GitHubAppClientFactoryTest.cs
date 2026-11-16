using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Octokit;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Auth;
using PrDeploy.Api.Webhooks.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace PrDeploy.Api.Tests.Webhooks.Events;

/// <summary>
/// Contains tests that verify the behavior of the git Hub App Client Factory Test.
/// </summary>
public class GitHubAppClientFactoryTest
{
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// Verifies the expected behavior for this scenario: creates Signed Jwt For App.
    /// </summary>
    [Fact]
    public void CreateAppJwt_CreatesSignedJwtForApp()
    {
        var factory = CreateFactory((_, _) => throw new InvalidOperationException());

        var jwt = factory.CreateAppJwt();

        var parts = jwt.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.True(_rsa.VerifyData(Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"), Base64UrlDecode(parts[2]),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

        using var header = JsonDocument.Parse(Base64UrlDecode(parts[0]));
        Assert.Equal("RS256", header.RootElement.GetProperty("alg").GetString());

        using var payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
        var now = _time.GetUtcNow().ToUnixTimeSeconds();
        Assert.Equal("12345", payload.RootElement.GetProperty("iss").GetString());
        Assert.Equal(now - 60, payload.RootElement.GetProperty("iat").GetInt64());
        Assert.Equal(now + 540, payload.RootElement.GetProperty("exp").GetInt64());
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: caches Installation Tokens Until Near Expiry.
    /// </summary>
    [Fact]
    public async Task CreateInstallationClientAsync_CachesInstallationTokensUntilNearExpiry()
    {
        var created = new List<long>();
        var factory = CreateFactory((_, id) =>
        {
            created.Add(id);
            return Task.FromResult(GitHubModels.FromJson<AccessToken>(
                $$"""{ "token": "token-{{created.Count}}", "expires_at": "{{_time.GetUtcNow().AddHours(1):O}}" }"""));
        });

        var client = await factory.CreateInstallationClientAsync(42);
        await factory.CreateInstallationClientAsync(42);
        Assert.Equal("token-1", client.Connection.Credentials.Password);
        Assert.Equal([42L], created);

        // Refreshed within 5 minutes of expiring.
        _time.Advance(TimeSpan.FromMinutes(56));
        client = await factory.CreateInstallationClientAsync(42);
        Assert.Equal("token-2", client.Connection.Credentials.Password);
    }

    /// <summary>
    /// Creates the Factory from the supplied inputs.
    /// </summary>
    /// <param name="createToken">Value supplied for create Token.</param>
    /// <returns>The value produced by this operation.</returns>
    private GitHubAppClientFactory CreateFactory(Func<IGitHubClient, long, Task<AccessToken>> createToken)
    {
        var options = MsOptions.Create(new GitHubAppOptions { AppId = "12345", PrivateKey = _rsa.ExportRSAPrivateKeyPem() });
        return new GitHubAppClientFactory(options, _time, createToken);
    }

    /// <summary>
    /// Performs the base64 Url Decode operation and returns its result.
    /// </summary>
    /// <param name="value">Value to store or process.</param>
    /// <returns>The value produced by this operation.</returns>
    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
    }
}

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Octokit;
using PrDeploy.Api.Webhooks.Options;

namespace PrDeploy.Api.Webhooks.Auth;

/// <summary>
/// Defines the contract for git Hub App Client Factory operations.
/// </summary>
public interface IGitHubAppClientFactory
{
    /// <summary>
    /// Creates a GitHub client authenticated as the GitHub App installation that sent an event.
    /// </summary>
    Task<IGitHubClient> CreateInstallationClientAsync(long installationId);
}

/// <summary>
/// Authenticates as the GitHub App with a signed JWT and exchanges it for cached installation tokens.
/// </summary>
public class GitHubAppClientFactory : IGitHubAppClientFactory
{
    private static readonly ProductHeaderValue ProductHeader = new("prdeploy");

    // Installation tokens last an hour, refresh them early so they do not expire mid-flow.
    private static readonly TimeSpan TokenRefreshMargin = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<long, AccessToken> _tokens = new();
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly GitHubAppOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Func<IGitHubClient, long, Task<AccessToken>> _createInstallationToken;
    private RSA? _privateKey;

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="options">Object containing the options data used by the operation.</param>
    /// <param name="timeProvider">Object containing the time provider data used by the operation.</param>
    public GitHubAppClientFactory(IOptions<GitHubAppOptions> options, TimeProvider timeProvider)
        : this(options, timeProvider, (client, id) => client.GitHubApps.CreateInstallationToken(id))
    {
    }

    /// <summary>
    /// Initializes the instance with its required dependencies.
    /// </summary>
    /// <param name="options">Object containing the options data used by the operation.</param>
    /// <param name="timeProvider">Object containing the time provider data used by the operation.</param>
    /// <param name="createInstallationToken">Object containing the create installation token data used by the operation.</param>
    internal GitHubAppClientFactory(IOptions<GitHubAppOptions> options, TimeProvider timeProvider,
        Func<IGitHubClient, long, Task<AccessToken>> createInstallationToken)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _createInstallationToken = createInstallationToken;
    }

    /// <summary>
    /// Creates the installation client from the supplied inputs.
    /// </summary>
    /// <param name="installationId">Numeric value used for the installation id.</param>
    /// <returns>The object created from the supplied inputs.</returns>
    public async Task<IGitHubClient> CreateInstallationClientAsync(long installationId)
    {
        var token = await GetInstallationTokenAsync(installationId);
        return new GitHubClient(ProductHeader)
        {
            Credentials = new Credentials(token.Token)
        };
    }

    /// <summary>
    /// Creates the app JWT from the supplied inputs.
    /// </summary>
    /// <returns>The object created from the supplied inputs.</returns>
    internal string CreateAppJwt()
    {
        if (string.IsNullOrWhiteSpace(_options.AppId))
        {
            throw new InvalidOperationException(
                $"{GitHubAppOptions.SectionName}:{nameof(GitHubAppOptions.AppId)} must be configured.");
        }

        // SourceRef: https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-json-web-token-jwt-for-a-github-app
        var now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iat = now - 60, // Allow for clock drift.
            exp = now + 540, // Maximum is 10 minutes.
            iss = _options.AppId
        }));

        var unsigned = $"{header}.{payload}";
        var signature = GetPrivateKey().SignData(Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return $"{unsigned}.{Base64UrlEncode(signature)}";
    }

    /// <summary>
    /// Retrieves the installation token.
    /// </summary>
    /// <param name="installationId">Numeric value used for the installation id.</param>
    /// <returns>Requested value, or its default when no value is available.</returns>
    private async Task<AccessToken> GetInstallationTokenAsync(long installationId)
    {
        if (TryGetCachedToken(installationId, out var token))
        {
            return token;
        }

        await _tokenLock.WaitAsync();
        try
        {
            if (TryGetCachedToken(installationId, out token))
            {
                return token;
            }

            var appClient = new GitHubClient(ProductHeader)
            {
                Credentials = new Credentials(CreateAppJwt(), AuthenticationType.Bearer)
            };

            token = await _createInstallationToken(appClient, installationId);
            _tokens[installationId] = token;
            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Attempts to get cached token; the result indicates whether it succeeded.
    /// </summary>
    /// <param name="installationId">Numeric value used for the installation id.</param>
    /// <param name="token">Authentication token used for the operation.</param>
    /// <returns>True when the requested condition is met; otherwise, false.</returns>
    private bool TryGetCachedToken(long installationId, out AccessToken token)
    {
        return _tokens.TryGetValue(installationId, out token!)
               && token.ExpiresAt - TokenRefreshMargin > _timeProvider.GetUtcNow();
    }

    /// <summary>
    /// Retrieves the private key.
    /// </summary>
    /// <returns>Requested value, or its default when no value is available.</returns>
    private RSA GetPrivateKey()
    {
        if (_privateKey != null)
        {
            return _privateKey;
        }

        var pem = _options.PrivateKey;
        if (string.IsNullOrWhiteSpace(pem) && !string.IsNullOrWhiteSpace(_options.PrivateKeyPath))
        {
            pem = File.ReadAllText(_options.PrivateKeyPath);
        }

        if (string.IsNullOrWhiteSpace(pem))
        {
            throw new InvalidOperationException(
                $"{GitHubAppOptions.SectionName}:{nameof(GitHubAppOptions.PrivateKey)} or " +
                $"{GitHubAppOptions.SectionName}:{nameof(GitHubAppOptions.PrivateKeyPath)} must be configured.");
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        _privateKey = rsa;
        return rsa;
    }

    /// <summary>
    /// Performs the base64 URL encode operation and returns its result.
    /// </summary>
    /// <param name="bytes">Object containing the bytes data used by the operation.</param>
    /// <returns>Text produced or selected by the operation.</returns>
    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

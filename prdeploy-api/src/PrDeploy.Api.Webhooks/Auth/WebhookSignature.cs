using System.Security.Cryptography;
using System.Text;

namespace PrDeploy.Api.Webhooks.Auth;

/// <summary>
/// Verifies the X-Hub-Signature-256 header GitHub sends with every webhook delivery.
/// </summary>
public static class WebhookSignature
{
    public const string HeaderName = "X-Hub-Signature-256";
    private const string Prefix = "sha256=";

    public static string Compute(string secret, byte[] body)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        return Prefix + Convert.ToHexStringLower(hash);
    }

    public static bool IsValid(string secret, byte[] body, string? signature)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signature)
            || !signature.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expected = Encoding.ASCII.GetBytes(Compute(secret, body));
        var actual = Encoding.ASCII.GetBytes(signature.ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}

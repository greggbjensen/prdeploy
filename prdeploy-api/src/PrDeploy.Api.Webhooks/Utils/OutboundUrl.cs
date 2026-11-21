using System.Net;
using System.Net.Sockets;

namespace PrDeploy.Api.Webhooks.Utils;

/// <summary>
/// Validates URLs taken from repository settings before the API sends requests to them.
/// </summary>
/// <remarks>
/// Anyone who can push to a repository can edit its settings, so these URLs are untrusted. Requiring HTTPS and
/// rejecting IP literals in internal ranges stops them from being used to reach cloud metadata endpoints or other
/// services on the cluster network. Host names are not resolved here, so this does not prevent DNS rebinding.
/// </remarks>
public static class OutboundUrl
{
    /// <summary>
    /// Checks whether a configured URL may be called.
    /// </summary>
    /// <param name="url">URL read from settings.</param>
    /// <param name="uri">The parsed URL when it is allowed.</param>
    /// <returns><c>true</c> when the URL is absolute HTTPS and does not target an internal IP address.</returns>
    public static bool TryCreate(string? url, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            || parsed.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(parsed.UserInfo)
            || string.Equals(parsed.IdnHost, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IPAddress.TryParse(parsed.IdnHost.Trim('[', ']'), out var address) && IsInternal(address))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    /// <summary>
    /// Checks whether an IP address is loopback, private, link-local, or otherwise not publicly routable.
    /// </summary>
    /// <param name="address">Address to check.</param>
    /// <returns><c>true</c> when the address is internal.</returns>
    private static bool IsInternal(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            100 => bytes[1] >= 64 && bytes[1] <= 127, // Carrier-grade NAT.
            169 => bytes[1] == 254, // Link-local, including cloud metadata endpoints.
            172 => bytes[1] >= 16 && bytes[1] <= 31,
            192 => bytes[1] == 168,
            0 => true,
            _ => false
        };
    }
}

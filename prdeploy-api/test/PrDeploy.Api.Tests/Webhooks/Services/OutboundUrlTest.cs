using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Tests.Webhooks.Services;

/// <summary>
/// Contains tests that verify which settings URLs the API is allowed to call.
/// </summary>
public class OutboundUrlTest
{
    /// <summary>
    /// Verifies the expected behavior for this scenario: public HTTPS URLs are allowed.
    /// </summary>
    /// <param name="url">URL to check.</param>
    [Theory]
    [InlineData("https://hooks.slack.com/services/T000/B000/XXXX")]
    [InlineData("https://myorg.atlassian.net")]
    [InlineData("https://8.8.8.8/hook")]
    [InlineData("https://172.32.0.1")]
    public void TryCreate_AllowsPublicHttpsUrls(string url)
    {
        Assert.True(OutboundUrl.TryCreate(url, out var uri));
        Assert.Equal(url, uri.OriginalString);
    }

    /// <summary>
    /// Verifies the expected behavior for this scenario: non-HTTPS, relative, credentialed, and internal addresses
    /// are rejected.
    /// </summary>
    /// <param name="url">URL to check.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("/relative/path")]
    [InlineData("http://hooks.slack.com/services/x")]
    [InlineData("file:///etc/passwd")]
    [InlineData("https://user:pass@hooks.slack.com/x")]
    [InlineData("https://localhost:8080")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://10.0.0.5")]
    [InlineData("https://172.16.0.1")]
    [InlineData("https://192.168.1.1")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("https://100.64.0.1")]
    [InlineData("https://0.0.0.0")]
    [InlineData("https://[::1]")]
    [InlineData("https://[fe80::1]")]
    [InlineData("https://[fd00::1]")]
    [InlineData("https://[::ffff:10.0.0.1]")]
    public void TryCreate_RejectsUnsafeUrls(string? url)
    {
        Assert.False(OutboundUrl.TryCreate(url, out _));
    }
}

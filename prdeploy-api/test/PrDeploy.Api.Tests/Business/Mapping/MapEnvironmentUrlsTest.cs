using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Tests.Business.Mapping;

/// <summary>
/// Verifies how <see cref="Map.EnvironmentUrls"/> builds the labelled links for an environment.
/// </summary>
public class MapEnvironmentUrlsTest
{
    /// <summary>
    /// Labelled URLs are returned in order with their labels.
    /// </summary>
    [Fact]
    public void ReturnsLabelledUrlsInOrder()
    {
        var environment = new EnvironmentSettings
        {
            Urls = new()
            {
                new() { Label = "Web", Url = "https://web.test" },
                new() { Label = "Admin", Url = "https://admin.test" }
            }
        };

        var urls = Map.EnvironmentUrls(environment);

        Assert.Equal(new[] { "Web", "Admin" }, urls.Select(u => u.Label));
        Assert.Equal(new[] { "https://web.test", "https://admin.test" }, urls.Select(u => u.Url));
    }

    /// <summary>
    /// The legacy single URL is shown when no labelled URLs exist.
    /// </summary>
    [Fact]
    public void FallsBackToLegacyUrlWhenNoUrlsConfigured()
    {
        var urls = Map.EnvironmentUrls(new EnvironmentSettings { Url = " https://legacy.test " });

        var url = Assert.Single(urls);
        Assert.Equal(Map.DefaultUrlLabel, url.Label);
        Assert.Equal("https://legacy.test", url.Url);
    }

    /// <summary>
    /// The legacy URL is ignored once labelled URLs are configured.
    /// </summary>
    [Fact]
    public void IgnoresLegacyUrlWhenUrlsConfigured()
    {
        var environment = new EnvironmentSettings
        {
            Url = "https://legacy.test",
            Urls = new() { new() { Label = "Web", Url = "https://web.test" } }
        };

        Assert.Equal("Web", Assert.Single(Map.EnvironmentUrls(environment)).Label);
    }

    /// <summary>
    /// Entries without a URL are dropped and a blank label falls back to the URL.
    /// </summary>
    [Fact]
    public void SkipsBlankUrlsAndDefaultsBlankLabel()
    {
        var environment = new EnvironmentSettings
        {
            Urls = new()
            {
                new() { Label = "Empty", Url = "  " },
                new() { Label = " ", Url = "https://web.test" }
            }
        };

        var url = Assert.Single(Map.EnvironmentUrls(environment));
        Assert.Equal("https://web.test", url.Label);
    }

    /// <summary>
    /// An environment with no URLs of any kind yields an empty list.
    /// </summary>
    [Fact]
    public void ReturnsEmptyWhenNothingConfigured()
    {
        Assert.Empty(Map.EnvironmentUrls(new EnvironmentSettings()));
    }
}

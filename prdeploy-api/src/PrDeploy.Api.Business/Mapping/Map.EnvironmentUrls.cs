using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Business.Mapping;

/// <summary>Maps environment settings to the links displayed for them.</summary>
public static partial class Map
{
    /// <summary>Label given to the legacy single <see cref="EnvironmentSettings.Url"/> when it is shown as a link.</summary>
    public const string DefaultUrlLabel = "URL";

    /// <summary>Builds the labelled URLs for an environment, skipping entries without a URL. A blank label falls back to the URL itself, and the legacy single URL is used only when no labelled URLs are configured.</summary>
    /// <param name="environment">Environment whose URLs are listed.</param>
    /// <returns>The URLs to display, empty when none are configured.</returns>
    public static List<EnvironmentUrl> EnvironmentUrls(EnvironmentSettings environment)
    {
        var urls = (environment.Urls ?? new List<EnvironmentUrl>())
            .Where(u => !string.IsNullOrWhiteSpace(u.Url))
            .Select(u => new EnvironmentUrl
            {
                Label = string.IsNullOrWhiteSpace(u.Label) ? u.Url.Trim() : u.Label.Trim(),
                Url = u.Url.Trim()
            })
            .ToList();

        if (urls.Count == 0 && !string.IsNullOrWhiteSpace(environment.Url))
        {
            urls.Add(new EnvironmentUrl { Label = DefaultUrlLabel, Url = environment.Url.Trim() });
        }

        return urls;
    }
}

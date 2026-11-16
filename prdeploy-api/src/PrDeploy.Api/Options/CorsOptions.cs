namespace PrDeploy.Api.Options;

/// <summary>
/// Defines configuration options for cors.
/// </summary>
public class CorsOptions
{
    /// <summary>
    /// Gets or sets the allowed origins.
    /// </summary>
    /// <value>Current allowed origins value.</value>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

namespace PrDeploy.Api.Options;

/// <summary>
/// Defines configuration options for the cors Options.
/// </summary>
public class CorsOptions
{
    /// <summary>
    /// Gets or sets the Allowed Origins.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

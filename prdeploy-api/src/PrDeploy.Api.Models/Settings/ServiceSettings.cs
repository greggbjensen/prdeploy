namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for the service Settings.
/// </summary>
public class ServiceSettings
{
    /// <summary>
    /// Gets or sets the Name.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Path.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Path { get; set; }
}

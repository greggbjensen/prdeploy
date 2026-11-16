namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for service.
/// </summary>
public class ServiceSettings
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>Current name value.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    /// <value>Current path value.</value>
    public string? Path { get; set; }
}

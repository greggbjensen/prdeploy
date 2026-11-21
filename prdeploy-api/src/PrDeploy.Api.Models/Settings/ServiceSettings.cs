namespace PrDeploy.Api.Models.Settings;
/// <summary>Stores configuration values for service.</summary>
public class ServiceSettings
{
    /// <summary>Gets or sets the name.</summary>
    /// <value>Current name value.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the path.</summary>
    /// <value>Current path value.</value>
    public string? Path { get; set; }
    /// <summary>Gets or sets the build workflow file name, such as <c>api-build.yaml</c>, that produces this service.</summary>
    /// <value>Current workflow file name, or <c>null</c> when the service is not tied to a workflow.</value>
    public string? Workflow { get; set; }
}

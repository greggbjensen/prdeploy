namespace PrDeploy.Api.Business.Options;

/// <summary>
/// Settings shared by the configurable deployment document stores.
/// </summary>
public sealed class DeploymentStoreOptions
{
    /// <summary>
    /// Gets or sets the common path prefix used to scope deployment values.
    /// </summary>
    public string DeploymentValuePathPrefix { get; set; } = "/prdeploy";
}

namespace PrDeploy.Api.Business.Options;

/// <summary>Settings shared by the configurable deployment document stores.</summary>
public sealed class DeploymentStoreOptions
{
    /// <summary>Gets or sets the deployment value path prefix.</summary>
    /// <value>Current deployment value path prefix value.</value>
    public string DeploymentValuePathPrefix { get; set; } = "/prdeploy";
}

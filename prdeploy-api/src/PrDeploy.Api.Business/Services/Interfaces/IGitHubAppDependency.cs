namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>
/// Defines the contract for git Hub App Dependency operations.
/// </summary>
public interface IGitHubAppDependency<T>
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>Current value value.</value>
    T Value { get; set; }
}

namespace PrDeploy.Api.Business.Services.Interfaces;

/// <summary>
/// Defines the contract for git Hub App Dependency operations.
/// </summary>
public interface IGitHubAppDependency<T>
{
    /// <summary>
    /// Gets or sets the Value.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    T Value { get; set; }
}

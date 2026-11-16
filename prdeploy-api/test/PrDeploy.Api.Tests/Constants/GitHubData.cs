using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Tests.Constants;
/// <summary>
/// Represents the git Hub Data used by the application.
/// </summary>
public class GitHubData
{
    /// <summary>
    /// Gets the Owner.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Owner => "greggbjensen";
    /// <summary>
    /// Gets the Repo.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string Repo => "prdeploy-example-repo";
    /// <summary>
    /// Gets the Mock Default Settings Path.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string MockDefaultSettingsPath => "Mocks/.default-prdeploy.yaml";
    /// <summary>
    /// Gets the Mock Repo Settings Path.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string MockRepoSettingsPath => "Mocks/.prdeploy.yaml";
}

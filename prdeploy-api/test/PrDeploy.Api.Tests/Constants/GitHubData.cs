using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Tests.Constants;
/// <summary>
/// Represents git hub data data used by the application.
/// </summary>
public class GitHubData
{
    /// <summary>
    /// Gets the owner.
    /// </summary>
    /// <value>Current owner value.</value>
    public string Owner => "greggbjensen";
    /// <summary>
    /// Gets the repo.
    /// </summary>
    /// <value>Current repo value.</value>
    public string Repo => "prdeploy-example-repo";
    /// <summary>
    /// Gets the mock default settings path.
    /// </summary>
    /// <value>Current mock default settings path value.</value>
    public string MockDefaultSettingsPath => "Mocks/.default-prdeploy.yaml";
    /// <summary>
    /// Gets the mock repo settings path.
    /// </summary>
    /// <value>Current mock repo settings path value.</value>
    public string MockRepoSettingsPath => "Mocks/.prdeploy.yaml";
}

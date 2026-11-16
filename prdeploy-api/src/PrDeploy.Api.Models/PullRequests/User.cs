namespace PrDeploy.Api.Models.PullRequests;
/// <summary>
/// Represents the deploy User used by the application.
/// </summary>
public class DeployUser
{
    /// <summary>
    /// Gets or sets the Name.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the Username.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Username { get; set; }
}

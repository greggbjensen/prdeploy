namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Represents the slack User used by the application.
/// </summary>
public class SlackUser
{
    /// <summary>
    /// Gets or sets the Name.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets the Email.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Email { get; set; }
    /// <summary>
    /// Gets or sets the Id.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Id { get; set; }
    /// <summary>
    /// Gets or sets the Username.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Username { get; set; }
}

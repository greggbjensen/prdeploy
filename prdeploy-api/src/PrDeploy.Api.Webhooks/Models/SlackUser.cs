namespace PrDeploy.Api.Webhooks.Models;

/// <summary>Represents Slack user data used by the application.</summary>
public class SlackUser
{
    /// <summary>Gets or sets the name.</summary>
    /// <value>Current name value.</value>
    public string? Name { get; set; }
    /// <summary>Gets or sets the email.</summary>
    /// <value>Current email value.</value>
    public string? Email { get; set; }
    /// <summary>Gets or sets the id.</summary>
    /// <value>Current id value.</value>
    public string? Id { get; set; }
    /// <summary>Gets or sets the username.</summary>
    /// <value>Current username value.</value>
    public string? Username { get; set; }
}

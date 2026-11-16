using HotChocolate.Types;
using HotChocolate;

namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for the slack Settings.
/// </summary>
public class SlackSettings
{
    /// <summary>
    /// Gets or sets the Token.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Token { get; set; }
    /// <summary>
    /// Gets or sets the Email Domain.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? EmailDomain { get; set; }
    /// <summary>
    /// Gets or sets the Email Aliases.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLType(typeof(AnyType))]
    public Dictionary<string, object>? EmailAliases { get; set; }
    /// <summary>
    /// Gets or sets the Webhooks.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public SlackWebHooksSettings? Webhooks { get; set; }
    /// <summary>
    /// Gets or sets the Notifications Enabled.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public bool? NotificationsEnabled { get; set; }
}

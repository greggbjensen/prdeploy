using HotChocolate.Types;
using HotChocolate;

namespace PrDeploy.Api.Models.Settings;
/// <summary>Stores configuration values for Slack.</summary>
public class SlackSettings
{
    /// <summary>Gets or sets the token.</summary>
    /// <value>Current token value.</value>
    public string? Token { get; set; }
    /// <summary>Gets or sets the email domain.</summary>
    /// <value>Current email domain value.</value>
    public string? EmailDomain { get; set; }
    /// <summary>Gets or sets the email aliases.</summary>
    /// <value>Current email aliases value.</value>
    [GraphQLType(typeof(AnyType))]
    public Dictionary<string, object>? EmailAliases { get; set; }
    /// <summary>Gets or sets the webhooks.</summary>
    /// <value>Current webhooks value.</value>
    public SlackWebHooksSettings? Webhooks { get; set; }
    /// <summary>Gets or sets the notifications enabled.</summary>
    /// <value>Current notifications enabled value.</value>
    public bool? NotificationsEnabled { get; set; }
}

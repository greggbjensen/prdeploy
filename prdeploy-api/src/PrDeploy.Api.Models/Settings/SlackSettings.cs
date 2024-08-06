using HotChocolate.Types;
using HotChocolate;

namespace PrDeploy.Api.Models.Settings;
public class SlackSettings
{
    public string? Token { get; set; }
    public string? EmailDomain { get; set; }
    [GraphQLType(typeof(AnyType))]
    public Dictionary<string, string>? EmailAliases { get; set; }
    public SlackWebHooksSettings? Webhooks { get; set; }
    public bool? NotificationsEnabled { get; set; }
}

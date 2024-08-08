
using HotChocolate.Types;
using HotChocolate;

namespace PrDeploy.Api.Models.Settings;
public class AutomationTestSettings
{
    public bool? Enabled { get; set; }
    public string? Workflow { get; set; }
    [GraphQLType(typeof(AnyType))]
    public Dictionary<string, object>? Inputs { get; set; }
}

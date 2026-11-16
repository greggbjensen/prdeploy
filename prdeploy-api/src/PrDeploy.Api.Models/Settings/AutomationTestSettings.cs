
using HotChocolate.Types;
using HotChocolate;

namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for automation test.
/// </summary>
public class AutomationTestSettings
{
    /// <summary>
    /// Gets or sets the enabled.
    /// </summary>
    /// <value>Current enabled value.</value>
    public bool? Enabled { get; set; }
    /// <summary>
    /// Gets or sets the workflow.
    /// </summary>
    /// <value>Current workflow value.</value>
    public string? Workflow { get; set; }
    /// <summary>
    /// Gets or sets the inputs.
    /// </summary>
    /// <value>Current inputs value.</value>
    [GraphQLType(typeof(AnyType))]
    public Dictionary<string, object>? Inputs { get; set; }
}

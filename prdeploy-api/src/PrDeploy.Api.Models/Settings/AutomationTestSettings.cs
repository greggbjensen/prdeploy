
using HotChocolate.Types;
using HotChocolate;

namespace PrDeploy.Api.Models.Settings;
/// <summary>
/// Stores configuration values for the automation Test Settings.
/// </summary>
public class AutomationTestSettings
{
    /// <summary>
    /// Gets or sets the Enabled.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public bool? Enabled { get; set; }
    /// <summary>
    /// Gets or sets the Workflow.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    public string? Workflow { get; set; }
    /// <summary>
    /// Gets or sets the Inputs.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLType(typeof(AnyType))]
    public Dictionary<string, object>? Inputs { get; set; }
}

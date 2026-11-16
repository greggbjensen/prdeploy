using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments;

/// <summary>
/// Represents the environment used by the application.
/// </summary>
[GraphQLDescription("Deployment environment such as Dev, Stage, and Prod.")]
public class Environment
{
    /// <summary>
    /// Gets or sets the Name.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLType(typeof(IdType))]
    [GraphQLDescription("Readable name of the environment.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLDescription("Full URL of the environment.")]
    public string? Url { get; set; }
}

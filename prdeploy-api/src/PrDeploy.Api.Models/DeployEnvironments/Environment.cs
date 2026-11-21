using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments;

/// <summary>Represents environment data used by the application.</summary>
[GraphQLDescription("Deployment environment such as Dev, Stage, and Prod.")]
public class Environment
{
    /// <summary>Gets or sets the name.</summary>
    /// <value>Current name value.</value>
    [GraphQLType(typeof(IdType))]
    [GraphQLDescription("Readable name of the environment.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL.</summary>
    /// <value>Current URL value.</value>
    [GraphQLDescription("Full URL of the environment.")]
    public string? Url { get; set; }
}

using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments;

[GraphQLDescription("Deployment environment such as Dev, Stage, and Prod.")]
public class Environment
{
    [GraphQLType(typeof(IdType))]
    [GraphQLDescription("Readable name of the environment.")]
    public string Name { get; set; } = string.Empty;

    [GraphQLDescription("Full URL of the environment.")]
    public string? Url { get; set; }
}

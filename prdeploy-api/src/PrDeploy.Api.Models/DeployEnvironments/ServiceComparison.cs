using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments
{
    [GraphQLDescription("Comparison of services between a source and target environment.")]
    public class ServiceComparison
    {
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Name of the service deployed.")]
        public string Name { get; set; } = string.Empty;

        [GraphQLDescription("Workflow run ID of the source service.")]
        public int SourceRunId { get; set; }

        [GraphQLDescription("Version of the source service.")]
        public string SourceVersion { get; set; } = string.Empty;

        [GraphQLDescription("Workflow run ID of the target service.")]
        public int TargetRunId { get; set; }

        [GraphQLDescription("Version of the target service.")]
        public string TargetVersion { get; set; } = string.Empty;
    }
}

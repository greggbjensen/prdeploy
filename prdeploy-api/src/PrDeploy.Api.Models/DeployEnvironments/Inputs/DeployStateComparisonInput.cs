using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    public class DeployStateComparisonInput
    {
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;

        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository being accessed within the owner.")]
        public string Repo { get; set; } = string.Empty;

        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment to compare from.")]
        public string SourceEnvironment { get; set; } = string.Empty;

        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment to compare to.")]
        public string TargetEnvironment { get; set; } = string.Empty;
    }
}

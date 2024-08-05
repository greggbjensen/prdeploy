using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments
{
    public class DeployStateComparison
    {
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Source environment to compare.")]
        public string SourceEnvironment { get; set; } = string.Empty;

        [GraphQLDescription("Source environment current pull request number.")]
        public int SourcePullNumber { get; set; }

        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Target environment to compare to, Prod unless source is Prod, then Stable.")]
        public string TargetEnvironment { get; set; } = string.Empty;


        [GraphQLDescription("Target environment current pull request number.")]
        public int TargetPullNumber { get; set; }

        [GraphQLDescription("List of service comparisons.")]
        public List<ServiceComparison> ServiceComparisons { get; set; } = null!;
    }
}

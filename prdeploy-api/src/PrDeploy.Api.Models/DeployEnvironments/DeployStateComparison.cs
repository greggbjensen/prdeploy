using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments
{
    /// <summary>Represents deploy state comparison data used by the application.</summary>
    public class DeployStateComparison
    {
        /// <summary>Gets or sets the source environment.</summary>
        /// <value>Current source environment value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Source environment to compare.")]
        public string SourceEnvironment { get; set; } = string.Empty;

        /// <summary>Gets or sets the source pull number.</summary>
        /// <value>Current source pull number value.</value>
        [GraphQLDescription("Source environment current pull request number.")]
        public int SourcePullNumber { get; set; }

        /// <summary>Gets or sets the target environment.</summary>
        /// <value>Current target environment value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Target environment to compare to, Prod unless source is Prod, then Stable.")]
        public string TargetEnvironment { get; set; } = string.Empty;


        /// <summary>Gets or sets the target pull number.</summary>
        /// <value>Current target pull number value.</value>
        [GraphQLDescription("Target environment current pull request number.")]
        public int TargetPullNumber { get; set; }

        /// <summary>Gets or sets the service comparisons.</summary>
        /// <value>Current service comparisons value.</value>
        [GraphQLDescription("List of service comparisons.")]
        public List<ServiceComparison> ServiceComparisons { get; set; } = null!;
    }
}

using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments
{
    /// <summary>
    /// Represents the deploy State Comparison used by the application.
    /// </summary>
    public class DeployStateComparison
    {
        /// <summary>
        /// Gets or sets the Source Environment.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Source environment to compare.")]
        public string SourceEnvironment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Source Pull Number.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("Source environment current pull request number.")]
        public int SourcePullNumber { get; set; }

        /// <summary>
        /// Gets or sets the Target Environment.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Target environment to compare to, Prod unless source is Prod, then Stable.")]
        public string TargetEnvironment { get; set; } = string.Empty;


        /// <summary>
        /// Gets or sets the Target Pull Number.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("Target environment current pull request number.")]
        public int TargetPullNumber { get; set; }

        /// <summary>
        /// Gets or sets the Service Comparisons.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("List of service comparisons.")]
        public List<ServiceComparison> ServiceComparisons { get; set; } = null!;
    }
}

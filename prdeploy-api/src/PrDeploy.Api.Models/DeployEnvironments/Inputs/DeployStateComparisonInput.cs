using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    /// <summary>
    /// Carries input values for the deploy State Comparison Input.
    /// </summary>
    [GraphQLDescription("Input for retrieving the deploy state comparison.")]
    public class DeployStateComparisonInput
    {
        /// <summary>
        /// Gets or sets the Owner.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Repo.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository being accessed within the owner.")]
        public string Repo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Source Environment.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment to compare from.")]
        public string SourceEnvironment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Target Environment.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment to compare to.")]
        public string TargetEnvironment { get; set; } = string.Empty;
    }
}

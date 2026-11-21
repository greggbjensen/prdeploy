using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    /// <summary>Carries input values for deploy state comparison operations.</summary>
    [GraphQLDescription("Input for retrieving the deploy state comparison.")]
    public class DeployStateComparisonInput
    {
        /// <summary>Gets or sets the owner.</summary>
        /// <value>Current owner value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;

        /// <summary>Gets or sets the repo.</summary>
        /// <value>Current repo value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository being accessed within the owner.")]
        public string Repo { get; set; } = string.Empty;

        /// <summary>Gets or sets the source environment.</summary>
        /// <value>Current source environment value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment to compare from.")]
        public string SourceEnvironment { get; set; } = string.Empty;

        /// <summary>Gets or sets the target environment.</summary>
        /// <value>Current target environment value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment to compare to.")]
        public string TargetEnvironment { get; set; } = string.Empty;
    }
}

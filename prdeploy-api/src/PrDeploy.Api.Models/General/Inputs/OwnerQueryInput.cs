using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.General.Inputs
{
    /// <summary>
    /// Carries input values for owner query operations.
    /// </summary>
    [GraphQLDescription("Input for a general owner query.")]
    public class OwnerQueryInput
    {
        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>Current owner value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;
    }
}

using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.General.Inputs
{
    /// <summary>
    /// Carries input values for the owner Query Input.
    /// </summary>
    [GraphQLDescription("Input for a general owner query.")]
    public class OwnerQueryInput
    {
        /// <summary>
        /// Gets or sets the Owner.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;
    }
}

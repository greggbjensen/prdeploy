using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.General.Inputs
{
    /// <summary>
    /// Carries input values for the repo Query Input.
    /// </summary>
    [GraphQLDescription("Input for a general owner and repo query.")]
    public class RepoQueryInput
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
    }
}

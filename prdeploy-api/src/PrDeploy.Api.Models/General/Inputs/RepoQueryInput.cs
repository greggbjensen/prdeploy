using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.General.Inputs
{
    /// <summary>Carries input values for repo query operations.</summary>
    [GraphQLDescription("Input for a general owner and repo query.")]
    public class RepoQueryInput
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
    }
}

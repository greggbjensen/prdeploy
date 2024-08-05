using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.General.Inputs
{
    [GraphQLDescription("Input for a general owner and repo query.")]
    public class RepoQueryInput
    {
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;

        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository being accessed within the owner.")]
        public string Repo { get; set; } = string.Empty;
    }
}

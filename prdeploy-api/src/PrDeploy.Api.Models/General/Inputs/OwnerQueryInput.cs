using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.General.Inputs
{
    [GraphQLDescription("Input for a general owner query.")]
    public class OwnerQueryInput
    {
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;
    }
}

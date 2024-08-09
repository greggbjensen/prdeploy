using HotChocolate.Types;
using HotChocolate;

namespace PrDeploy.Api.Models.Settings.Compare
{
    public class OwnerRepoDictionary
    {
        [GraphQLType(typeof(AnyType))]
        public Dictionary<string, object>? Owner { get; set; }
        [GraphQLType(typeof(AnyType))]
        public Dictionary<string, object>? Repo { get; set; }
    }
}

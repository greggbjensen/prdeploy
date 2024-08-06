using HotChocolate.Types;
using HotChocolate;

namespace PrDeploy.Api.Models.Settings.Compare
{
    public class OwnerRepoDictionary
    {
        [GraphQLType(typeof(AnyType))]
        public Dictionary<string, string> Owner { get; set; }
        [GraphQLType(typeof(AnyType))]
        public Dictionary<string, string> Repo { get; set; }
    }
}

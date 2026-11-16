using HotChocolate.Types;
using HotChocolate;

namespace PrDeploy.Api.Models.Settings.Compare
{
    /// <summary>
    /// Represents owner repo dictionary data used by the application.
    /// </summary>
    public class OwnerRepoDictionary
    {
        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>Current owner value.</value>
        [GraphQLType(typeof(AnyType))]
        public Dictionary<string, object>? Owner { get; set; }
        /// <summary>
        /// Gets or sets the repo.
        /// </summary>
        /// <value>Current repo value.</value>
        [GraphQLType(typeof(AnyType))]
        public Dictionary<string, object>? Repo { get; set; }
    }
}

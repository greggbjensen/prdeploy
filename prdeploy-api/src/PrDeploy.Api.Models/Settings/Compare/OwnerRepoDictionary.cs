using HotChocolate.Types;
using HotChocolate;

namespace PrDeploy.Api.Models.Settings.Compare
{
    /// <summary>
    /// Represents the owner Repo Dictionary used by the application.
    /// </summary>
    public class OwnerRepoDictionary
    {
        /// <summary>
        /// Gets or sets the Owner.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(AnyType))]
        public Dictionary<string, object>? Owner { get; set; }
        /// <summary>
        /// Gets or sets the Repo.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(AnyType))]
        public Dictionary<string, object>? Repo { get; set; }
    }
}

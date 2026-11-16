using HotChocolate;

namespace PrDeploy.Api.Models.OwnerRepo
{
    /// <summary>
    /// Represents the owner Repos used by the application.
    /// </summary>
    [GraphQLDescription("Set of owner and associated repos.")]
    public class OwnerRepos
    {
        /// <summary>
        /// Gets or sets the Owner.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("Owner or organization of a set of repo.")]
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Repos.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("List of repository names for owner.")]
        public List<string> Repos { get; set; } = null!;
    }
}

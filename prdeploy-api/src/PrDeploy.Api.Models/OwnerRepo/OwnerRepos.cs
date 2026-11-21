using HotChocolate;

namespace PrDeploy.Api.Models.OwnerRepo
{
    /// <summary>Represents owner repos data used by the application.</summary>
    [GraphQLDescription("Set of owner and associated repos.")]
    public class OwnerRepos
    {
        /// <summary>Gets or sets the owner.</summary>
        /// <value>Current owner value.</value>
        [GraphQLDescription("Owner or organization of a set of repo.")]
        public string Owner { get; set; } = string.Empty;

        /// <summary>Gets or sets the repos.</summary>
        /// <value>Current repos value.</value>
        [GraphQLDescription("List of repository names for owner.")]
        public List<string> Repos { get; set; } = null!;
    }
}

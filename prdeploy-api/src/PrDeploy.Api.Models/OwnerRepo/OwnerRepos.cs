using HotChocolate;

namespace PrDeploy.Api.Models.OwnerRepo
{
    [GraphQLDescription("Set of owner and associated repos.")]
    public class OwnerRepos
    {
        [GraphQLDescription("Owner or organization of a set of repo.")]
        public string Owner { get; set; } = string.Empty;

        [GraphQLDescription("List of repository names for owner.")]
        public List<string> Repos { get; set; } = null!;
    }
}

using HotChocolate;

namespace PrDeploy.Api.Models.OwnerRepo;

[GraphQLDescription("Owner and repo name of a repository.")]
public class Repository
{
    [GraphQLDescription("Owner or organization of a repo.")]
    public string Owner { get; set; } = string.Empty;

    [GraphQLDescription("Name of the repository.")]
    public string Repo { get; set; } = string.Empty;
}

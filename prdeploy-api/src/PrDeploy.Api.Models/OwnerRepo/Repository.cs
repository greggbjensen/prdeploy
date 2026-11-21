using HotChocolate;

namespace PrDeploy.Api.Models.OwnerRepo;

/// <summary>Represents repository data used by the application.</summary>
[GraphQLDescription("Owner and repo name of a repository.")]
public class Repository
{
    /// <summary>Gets or sets the owner.</summary>
    /// <value>Current owner value.</value>
    [GraphQLDescription("Owner or organization of a repo.")]
    public string Owner { get; set; } = string.Empty;

    /// <summary>Gets or sets the repo.</summary>
    /// <value>Current repo value.</value>
    [GraphQLDescription("Name of the repository.")]
    public string Repo { get; set; } = string.Empty;
}

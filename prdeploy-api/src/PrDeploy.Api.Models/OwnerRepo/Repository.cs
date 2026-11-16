using HotChocolate;

namespace PrDeploy.Api.Models.OwnerRepo;

/// <summary>
/// Represents the repository used by the application.
/// </summary>
[GraphQLDescription("Owner and repo name of a repository.")]
public class Repository
{
    /// <summary>
    /// Gets or sets the Owner.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLDescription("Owner or organization of a repo.")]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Repo.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLDescription("Name of the repository.")]
    public string Repo { get; set; } = string.Empty;
}

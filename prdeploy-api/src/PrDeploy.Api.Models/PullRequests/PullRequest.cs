using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.PullRequests;

/// <summary>
/// Represents the pull Request used by the application.
/// </summary>
[GraphQLDescription("Pull request to deploy and merge code.")]
public class PullRequest
{
    /// <summary>
    /// Gets or sets the Number.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLDescription("Pull request number.")]
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the Title.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLDescription("Pull request title.")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the Url.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLDescription("URL for displaying the pull request HTML.")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the Body.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLDescription("Pull request body as markdown.")]
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the Updated At.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLDescription("The date and time the pull request deployment was last updated.")]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the User.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    [GraphQLDescription("Login for user the pull request was created by.")]
    public DeployUser? User { get; set; }
}

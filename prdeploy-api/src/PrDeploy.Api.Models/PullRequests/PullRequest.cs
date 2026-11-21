using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.PullRequests;

/// <summary>Represents pull request data used by the application.</summary>
[GraphQLDescription("Pull request to deploy and merge code.")]
public class PullRequest
{
    /// <summary>Gets or sets the number.</summary>
    /// <value>Current number value.</value>
    [GraphQLDescription("Pull request number.")]
    public int Number { get; set; }

    /// <summary>Gets or sets the title.</summary>
    /// <value>Current title value.</value>
    [GraphQLDescription("Pull request title.")]
    public string? Title { get; set; }

    /// <summary>Gets or sets the URL.</summary>
    /// <value>Current URL value.</value>
    [GraphQLDescription("URL for displaying the pull request HTML.")]
    public string? Url { get; set; }

    /// <summary>Gets or sets the body.</summary>
    /// <value>Current body value.</value>
    [GraphQLDescription("Pull request body as markdown.")]
    public string? Body { get; set; }

    /// <summary>Gets or sets the updated at.</summary>
    /// <value>Current updated at value.</value>
    [GraphQLDescription("The date and time the pull request deployment was last updated.")]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets or sets the user.</summary>
    /// <value>Current user value.</value>
    [GraphQLDescription("Login for user the pull request was created by.")]
    public DeployUser? User { get; set; }
}

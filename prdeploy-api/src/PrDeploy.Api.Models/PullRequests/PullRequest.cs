using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.PullRequests;

[GraphQLDescription("Pull request to deploy and merge code.")]
public class PullRequest
{
    [GraphQLDescription("Pull request number.")]
    public int Number { get; set; }

    [GraphQLDescription("Pull request title.")]
    public string? Title { get; set; }

    [GraphQLDescription("URL for displaying the pull request HTML.")]
    public string? Url { get; set; }

    [GraphQLDescription("Pull request body as markdown.")]
    public string? Body { get; set; }

    [GraphQLDescription("The date and time the pull request deployment was last updated.")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [GraphQLDescription("Login for user the pull request was created by.")]
    public DeployUser? User { get; set; }
}

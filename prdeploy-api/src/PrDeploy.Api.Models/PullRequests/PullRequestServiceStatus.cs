using HotChocolate;

namespace PrDeploy.Api.Models.PullRequests;

/// <summary>A configured service and whether a pull request already triggers its build.</summary>
[GraphQLDescription("A configured service and whether the pull request already triggers its build.")]
public class PullRequestServiceStatus
{
    /// <summary>Gets or sets the configured service name.</summary>
    /// <value>Name of the service as configured in the repository service settings.</value>
    [GraphQLDescription("Name of the service as configured in the repository service settings.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the pull request already triggers the service's build.</summary>
    /// <value><c>true</c> when the service's build workflow has run for the pull request's head commit.</value>
    [GraphQLDescription("True when the service's build workflow has run for the pull request's head commit.")]
    public bool Triggered { get; set; }
}

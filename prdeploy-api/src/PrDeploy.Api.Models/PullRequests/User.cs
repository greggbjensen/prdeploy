using HotChocolate;

namespace PrDeploy.Api.Models.PullRequests;
/// <summary>Represents deploy user data used by the application.</summary>
public class DeployUser
{
    /// <summary>Gets or sets the name.</summary>
    /// <value>Current name value.</value>
    public string? Name { get; set; }

    /// <summary>Gets or sets the username.</summary>
    /// <value>Current username value.</value>
    public string? Username { get; set; }

    /// <summary>Gets or sets the URL of the user's GitHub avatar image.</summary>
    /// <value>Current avatar URL value.</value>
    [GraphQLDescription("URL of the user's GitHub avatar image.")]
    public string? AvatarUrl { get; set; }

    /// <summary>Gets or sets the URL of the user's GitHub profile page.</summary>
    /// <value>Current profile URL value.</value>
    [GraphQLDescription("URL of the user's GitHub profile page.")]
    public string? Url { get; set; }
}

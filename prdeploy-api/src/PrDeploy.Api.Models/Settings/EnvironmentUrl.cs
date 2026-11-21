using HotChocolate;

namespace PrDeploy.Api.Models.Settings;

/// <summary>A labelled link for an environment, such as "Web" or "Admin", shown in the deployments table.</summary>
[GraphQLDescription("Labelled URL of an environment.")]
public class EnvironmentUrl
{
    /// <summary>Gets or sets the text displayed for the link.</summary>
    /// <value>Current label value.</value>
    [GraphQLDescription("Text displayed for the link.")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the full URL the link points to.</summary>
    /// <value>Current URL value.</value>
    [GraphQLDescription("Full URL of the link.")]
    public string Url { get; set; } = string.Empty;
}

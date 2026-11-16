using HotChocolate;

namespace PrDeploy.Api.Models.General;

/// <summary>
/// Represents a response containing the status Response.
/// </summary>
[GraphQLDescription("Simple status response from a mutation.")]
public class StatusResponse
{
    /// <summary>
    /// Gets or sets the success.
    /// </summary>
    /// <value>Current success value.</value>
    [GraphQLDescription("True if the mutation was successful; otherwise false.")]
    public bool Success { get; set; } = true;
}

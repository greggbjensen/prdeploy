using HotChocolate;

namespace PrDeploy.Api.Models.General;

/// <summary>Represents a response containing the status Response.</summary>
[GraphQLDescription("Simple status response from a mutation.")]
public class StatusResponse
{
    /// <summary>Gets or sets the success.</summary>
    /// <value>Current success value.</value>
    [GraphQLDescription("True if the mutation was successful; otherwise false.")]
    public bool Success { get; set; } = true;

    /// <summary>Gets or sets a user facing explanation, set when the mutation was rejected.</summary>
    /// <value>Reason the mutation did not succeed, or null when it succeeded.</value>
    [GraphQLDescription("User facing reason the mutation was rejected; null when successful.")]
    public string? Message { get; set; }
}

using HotChocolate;

namespace PrDeploy.Api.Models;

[GraphQLDescription("Simple status response from a mutation.")]
public class StatusResponse
{
    [GraphQLDescription("True if the mutation was successful; otherwise false.")]
    public bool Success { get; set; } = true;
}

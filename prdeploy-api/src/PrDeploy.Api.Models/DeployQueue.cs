using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models
{
    [GraphQLDescription("Queue for a specific environment of pull requests waiting to be deployed.")]
    public class DeployQueue
    {
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment to list the queue for.")]
        public string? Environment { get; set; }

        [GraphQLDescription("Ordered list of pull requests waiting in queue.")]
        public List<PullRequest> PullRequests { get; set; } = new();
    }
}

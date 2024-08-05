using HotChocolate;
using HotChocolate.Types;
using PrDeploy.Api.Models.PullRequests;

namespace PrDeploy.Api.Models.DeployQueues
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

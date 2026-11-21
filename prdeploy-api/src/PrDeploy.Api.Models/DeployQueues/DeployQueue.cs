using HotChocolate;
using HotChocolate.Types;
using PrDeploy.Api.Models.PullRequests;

namespace PrDeploy.Api.Models.DeployQueues
{
    /// <summary>Represents deploy queue data used by the application.</summary>
    [GraphQLDescription("Queue for a specific environment of pull requests waiting to be deployed.")]
    public class DeployQueue
    {
        /// <summary>Gets or sets the environment.</summary>
        /// <value>Current environment value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment to list the queue for.")]
        public string? Environment { get; set; }

        /// <summary>Gets or sets the pull requests.</summary>
        /// <value>Current pull requests value.</value>
        [GraphQLDescription("Ordered list of pull requests waiting in queue.")]
        public List<PullRequest> PullRequests { get; set; } = new();
    }
}

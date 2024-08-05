using HotChocolate;
using HotChocolate.Types;
using PrDeploy.Api.Models.PullRequests;

namespace PrDeploy.Api.Models.DeployEnvironments
{
    [GraphQLDescription("Environment and current pull request information.")]
    public class DeployEnvironment
    {
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment that pull request is deployed to.")]
        public string? Name { get; set; }

        [GraphQLDescription("The full URL of the environment.")]
        public string? Url { get; set; }

        [GraphQLDescription("The color used for the icon representing the environment.")]
        public string? Color { get; set; }

        [GraphQLDescription("Current pull request in environment.")]
        public PullRequest? PullRequest { get; set; }

        [GraphQLDescription("A value indicating if the current environment is reserved for deploy.")]
        public bool Locked { get; set; }
    }
}

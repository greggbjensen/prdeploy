using HotChocolate;
using HotChocolate.Types;
using PrDeploy.Api.Models.PullRequests;

namespace PrDeploy.Api.Models.DeployEnvironments
{
    /// <summary>
    /// Represents deploy environment data used by the application.
    /// </summary>
    [GraphQLDescription("Environment and current pull request information.")]
    public class DeployEnvironment
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>Current name value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment that pull request is deployed to.")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>Current URL value.</value>
        [GraphQLDescription("The full URL of the environment.")]
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>Current color value.</value>
        [GraphQLDescription("The color used for the icon representing the environment.")]
        public string? Color { get; set; }

        /// <summary>
        /// Gets or sets the pull request.
        /// </summary>
        /// <value>Current pull request value.</value>
        [GraphQLDescription("Current pull request in environment.")]
        public PullRequest? PullRequest { get; set; }

        /// <summary>
        /// Gets or sets the locked.
        /// </summary>
        /// <value>Current locked value.</value>
        [GraphQLDescription("A value indicating if the current environment is reserved for deploy.")]
        public bool Locked { get; set; }
    }
}

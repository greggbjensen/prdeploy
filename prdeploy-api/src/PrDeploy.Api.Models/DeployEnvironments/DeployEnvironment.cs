using HotChocolate;
using HotChocolate.Types;
using PrDeploy.Api.Models.PullRequests;

namespace PrDeploy.Api.Models.DeployEnvironments
{
    /// <summary>
    /// Represents the deploy Environment used by the application.
    /// </summary>
    [GraphQLDescription("Environment and current pull request information.")]
    public class DeployEnvironment
    {
        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Environment that pull request is deployed to.")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the Url.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("The full URL of the environment.")]
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the Color.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("The color used for the icon representing the environment.")]
        public string? Color { get; set; }

        /// <summary>
        /// Gets or sets the Pull Request.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("Current pull request in environment.")]
        public PullRequest? PullRequest { get; set; }

        /// <summary>
        /// Gets or sets the Locked.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("A value indicating if the current environment is reserved for deploy.")]
        public bool Locked { get; set; }
    }
}

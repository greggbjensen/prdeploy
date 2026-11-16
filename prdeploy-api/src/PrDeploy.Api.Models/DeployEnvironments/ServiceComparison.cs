using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments
{
    /// <summary>
    /// Represents service comparison data used by the application.
    /// </summary>
    [GraphQLDescription("Comparison of services between a source and target environment.")]
    public class ServiceComparison
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>Current name value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Name of the service deployed.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source run id.
        /// </summary>
        /// <value>Current source run id value.</value>
        [GraphQLDescription("Workflow run ID of the source service.")]
        public long SourceRunId { get; set; }

        /// <summary>
        /// Gets or sets the source version.
        /// </summary>
        /// <value>Current source version value.</value>
        [GraphQLDescription("Version of the source service.")]
        public string SourceVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target run id.
        /// </summary>
        /// <value>Current target run id value.</value>
        [GraphQLDescription("Workflow run ID of the target service.")]
        public long TargetRunId { get; set; }

        /// <summary>
        /// Gets or sets the target version.
        /// </summary>
        /// <value>Current target version value.</value>
        [GraphQLDescription("Version of the target service.")]
        public string TargetVersion { get; set; } = string.Empty;
    }
}

using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.DeployEnvironments
{
    /// <summary>
    /// Represents the service Comparison used by the application.
    /// </summary>
    [GraphQLDescription("Comparison of services between a source and target environment.")]
    public class ServiceComparison
    {
        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Name of the service deployed.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Source Run Id.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("Workflow run ID of the source service.")]
        public long SourceRunId { get; set; }

        /// <summary>
        /// Gets or sets the Source Version.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("Version of the source service.")]
        public string SourceVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Target Run Id.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("Workflow run ID of the target service.")]
        public long TargetRunId { get; set; }

        /// <summary>
        /// Gets or sets the Target Version.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("Version of the target service.")]
        public string TargetVersion { get; set; } = string.Empty;
    }
}

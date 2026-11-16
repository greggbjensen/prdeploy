namespace PrDeploy.Api.Models.DeployEnvironments
{
    /// <summary>
    /// Coordinates operations for the deployed Service and its collaborators.
    /// </summary>
    public class DeployedService
    {
        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Run Id.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public long RunId { get; set; }
        /// <summary>
        /// Gets or sets the Version.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string Version { get; set; } = string.Empty;
    }
}

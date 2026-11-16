namespace PrDeploy.Api.Models.DeployEnvironments
{
    /// <summary>
    /// Coordinates deployed operations and their collaborators.
    /// </summary>
    public class DeployedService
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>Current name value.</value>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the run id.
        /// </summary>
        /// <value>Current run id value.</value>
        public long RunId { get; set; }
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>Current version value.</value>
        public string Version { get; set; } = string.Empty;
    }
}

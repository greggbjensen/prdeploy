namespace PrDeploy.Api.Models.DeployEnvironments
{
    /// <summary>
    /// Represents deploy state data used by the application.
    /// </summary>
    public class DeployState
    {
        /// <summary>
        /// Gets or sets the pull number.
        /// </summary>
        /// <value>Current pull number value.</value>
        public int PullNumber { get; set; }
        /// <summary>
        /// Gets or sets the services.
        /// </summary>
        /// <value>Current services value.</value>
        public List<DeployedService>? Services { get; set; } = null!;
    }
}

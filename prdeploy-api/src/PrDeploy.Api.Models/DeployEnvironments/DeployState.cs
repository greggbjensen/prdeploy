namespace PrDeploy.Api.Models.DeployEnvironments
{
    /// <summary>
    /// Represents the deploy State used by the application.
    /// </summary>
    public class DeployState
    {
        /// <summary>
        /// Gets or sets the Pull Number.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public int PullNumber { get; set; }
        /// <summary>
        /// Gets or sets the Services.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public List<DeployedService>? Services { get; set; } = null!;
    }
}

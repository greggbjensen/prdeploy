namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    /// <summary>
    /// Carries input values for the environment Deploy Input.
    /// </summary>
    public class EnvironmentDeployInput : PullDeployInput
    {
        /// <summary>
        /// Gets or sets the Force.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public bool? Force { get; set; } = null;
        /// <summary>
        /// Gets or sets the Retain.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public bool? Retain { get; set; } = null;
    }
}

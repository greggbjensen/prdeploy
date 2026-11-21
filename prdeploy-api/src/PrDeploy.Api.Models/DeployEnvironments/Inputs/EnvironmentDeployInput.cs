namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    /// <summary>Carries input values for environment deploy operations.</summary>
    public class EnvironmentDeployInput : PullDeployInput
    {
        /// <summary>Gets or sets the force.</summary>
        /// <value>Current force value.</value>
        public bool? Force { get; set; } = null;
        /// <summary>Gets or sets the retain.</summary>
        /// <value>Current retain value.</value>
        public bool? Retain { get; set; } = null;
    }
}

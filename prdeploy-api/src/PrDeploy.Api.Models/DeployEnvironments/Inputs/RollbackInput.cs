namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    /// <summary>
    /// Carries input values for the rollback Input.
    /// </summary>
    public class RollbackInput : PullDeployInput
    {
        /// <summary>
        /// Gets or sets the Count.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public int Count { get; set; } = 0;
    }
}

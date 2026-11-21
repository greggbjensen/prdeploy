namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    /// <summary>Carries input values for rollback operations.</summary>
    public class RollbackInput : PullDeployInput
    {
        /// <summary>Gets or sets the count.</summary>
        /// <value>Current count value.</value>
        public int Count { get; set; } = 0;
    }
}

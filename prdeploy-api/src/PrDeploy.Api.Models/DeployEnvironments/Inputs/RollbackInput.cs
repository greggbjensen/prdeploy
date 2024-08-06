namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    public class RollbackInput : PullDeployInput
    {
        public int Count { get; set; } = 0;
    }
}

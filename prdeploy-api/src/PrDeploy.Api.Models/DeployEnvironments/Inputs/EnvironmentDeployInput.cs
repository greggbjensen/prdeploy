namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    public class EnvironmentDeployInput : PullDeployInput
    {
        public bool Force { get; set; } = false;
        public bool Retain { get; set; } = false;
    }
}

namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    public class EnvironmentDeployInput : PullDeployInput
    {
        public bool? Force { get; set; } = null;
        public bool? Retain { get; set; } = null;
    }
}

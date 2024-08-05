namespace PrDeploy.Api.Models.DeployEnvironments
{
    public class DeployState
    {
        public int PullNumber { get; set; }
        public List<DeployedService> Services { get; set; } = null!;
    }
}

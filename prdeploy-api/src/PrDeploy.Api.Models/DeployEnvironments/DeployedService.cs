namespace PrDeploy.Api.Models.DeployEnvironments
{
    public class DeployedService
    {
        public string Name { get; set; } = string.Empty;
        public int RunId { get; set; }
        public string Version { get; set; } = string.Empty;
    }
}

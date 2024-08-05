namespace PrDeploy.Api.Models.DeployEnvironments
{
    public class DeployedService
    {
        public string Name { get; set; } = string.Empty;
        public long RunId { get; set; }
        public string Version { get; set; } = string.Empty;
    }
}

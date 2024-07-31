namespace PrDeploy.Api.Options;

public class DeployApiOptions
{
    public ServiceLifetime UserServiceLifetime { get; set; } = ServiceLifetime.Scoped;
}

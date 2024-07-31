using PrDeploy.Api.Models;

namespace PrDeploy.Api.Tests.Constants;

public class DeployEnvironmentData
{
    public DeployEnvironment Dev = new()
    {
        Name = "dev",
        Url = "https://dev.myorg.com",
        Color = "d4ac0d"
    };
}

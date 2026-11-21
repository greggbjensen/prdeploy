using PrDeploy.Api.Models;
using PrDeploy.Api.Models.DeployEnvironments;

namespace PrDeploy.Api.Tests.Constants;

/// <summary>Represents deploy environment data data used by the application.</summary>
public class DeployEnvironmentData
{
    public DeployEnvironment Dev = new()
    {
        Name = "dev",
        Url = "https://dev.myorg.com",
        Color = "#d4ac0d"
    };
}

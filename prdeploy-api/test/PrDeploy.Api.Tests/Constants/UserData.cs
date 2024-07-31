using PrDeploy.Api.Models;

namespace PrDeploy.Api.Tests.Constants;
public class DeployUserData
{
    public DeployUser JohnDoe = new()
    {
        Name = "John Doe",
        Username = "johndoe"
    };

    public DeployUser SteveWild = new()
    {
        Name = "Steve Wild",
        Username = "stevewild"
    };
}

using PrDeploy.Api.Models;
using PrDeploy.Api.Models.PullRequests;

namespace PrDeploy.Api.Tests.Constants;
/// <summary>
/// Represents the deploy User Data used by the application.
/// </summary>
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

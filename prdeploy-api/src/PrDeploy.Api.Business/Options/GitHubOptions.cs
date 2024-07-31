namespace PrDeploy.Api.Business.Options;

public class GitHubOptions
{
    public PrDeployOptions PrDeploy { get; set; } = new();
    public string Token { get; set; }
}

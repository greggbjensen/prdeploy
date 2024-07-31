using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Tests.Constants;
public class GitHubData : GitHubOptions
{
    public string Owner => "greggbjensen";
    public string Repo => "prdeploy-example-repo";
    public string MockDefaultSettingsPath => "Mocks/.default-prdeploy.yaml";
    public string MockRepoSettingsPath => "Mocks/.prdeploy.yaml";

    public GitHubData()
    {
        PrDeploy = new()
        {
            Owner = "greggbjensen",
            Repo = "prdeploy",
            DefaultSettingsPath = "prdeploy-webhooks/src/settings/.default-prdeploy.yaml",
            RepoSettingsPath = ".prdeploy.yaml"
        };

    }
}

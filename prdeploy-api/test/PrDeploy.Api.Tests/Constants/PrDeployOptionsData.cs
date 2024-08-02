using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Tests.Constants;
public class PrDeployOptionsData : PrDeployOptions
{
    public PrDeployOptionsData()
    {
        Owner = "greggbjensen";
        Repo = "prdeploy";
        DefaultSettingsPath = "prdeploy-webhooks/src/settings/.default-prdeploy.yaml";
        RepoSettingsPath = ".prdeploy.yaml";
    }
}

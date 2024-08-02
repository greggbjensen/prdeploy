using PrDeploy.Api.Business.Options;

namespace PrDeploy.Api.Tests.Constants;
public class GitHubData
{
    public string Owner => "greggbjensen";
    public string Repo => "prdeploy-example-repo";
    public string MockDefaultSettingsPath => "Mocks/.default-prdeploy.yaml";
    public string MockRepoSettingsPath => "Mocks/.prdeploy.yaml";
}

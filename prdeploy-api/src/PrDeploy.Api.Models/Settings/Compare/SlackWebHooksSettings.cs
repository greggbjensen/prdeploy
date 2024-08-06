namespace PrDeploy.Api.Models.Settings.Compare;

public class SlackWebHooksSettingsCompare
{
    public OwnerRepoValue<string> DeployUrl { get; set; } = null!;
    public OwnerRepoValue<string> ReleaseUrl { get; set; } = null!;
}

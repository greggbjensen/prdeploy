namespace PrDeploy.Api.Models.Settings.Compare;
public class BuildsSettingsCompare
{
    public OwnerRepoValue<string> CheckPattern { get; set; } = null!;
    public OwnerRepoValue<string> WorkflowPattern { get; set; } = null!;
}

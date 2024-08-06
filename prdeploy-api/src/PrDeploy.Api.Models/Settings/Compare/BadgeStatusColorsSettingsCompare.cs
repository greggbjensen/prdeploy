namespace PrDeploy.Api.Models.Settings.Compare;
public class BadgeStatusColorsSettingsCompare
{
    public OwnerRepoValue<string> Error { get; set; } = null!;
    public OwnerRepoValue<string> Warn { get; set; } = null!;
    public OwnerRepoValue<string> Success { get; set; } = null!;
    public OwnerRepoValue<string> Info { get; set; } = null!;
}

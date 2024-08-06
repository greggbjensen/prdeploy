namespace PrDeploy.Api.Models.Settings.Compare;
public class SlackSettingsCompare
{
    public OwnerRepoValue<string> Token { get; set; } = null!;
    public OwnerRepoValue<string> EmailDomain { get; set; } = null!;
    public OwnerRepoDictionary EmailAliases { get; set; } = null!;
    public SlackWebHooksSettingsCompare Webhooks { get; set; } = null!;
    public OwnerRepoValue<bool?> NotificationsEnabled { get; set; } = null!;
}

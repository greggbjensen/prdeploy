namespace PrDeploy.Api.Business.Models.Settings;
public class SlackSettings
{
    public string? Token { get; set; }
    public string? EmailDomain { get; set; }
    public EmailAliases? EmailAliases { get; set; }
    public SlackWebHooksSettings? Webhooks { get; set; }
    public bool? NotificationsEnabled { get; set; }
}

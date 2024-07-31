namespace PrDeploy.Api.Business.Models.Settings;
public class SlackSettings
{
    public string? Token { get; set; }
    public string? EmailDomain { get; set; } 
    public SlackChannelSettings? Channels { get; set; }
    public bool? NotificationsEnabled { get; set; }
}

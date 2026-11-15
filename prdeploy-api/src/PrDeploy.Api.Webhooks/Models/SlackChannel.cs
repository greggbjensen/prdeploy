namespace PrDeploy.Api.Webhooks.Models;

/// <summary>
/// Which configured Slack incoming webhook a message is posted to.
/// </summary>
public enum SlackChannel
{
    Deploy,
    Release
}

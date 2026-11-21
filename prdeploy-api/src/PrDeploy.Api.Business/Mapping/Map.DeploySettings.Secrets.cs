using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Business.Mapping;

/// <summary>Hides credential settings from API responses and restores them when masked values are saved back.</summary>
public static partial class Map
{
    /// <summary>Value returned in place of a configured secret. Saving this value keeps the stored secret.</summary>
    public const string SecretMask = "********";

    /// <summary>Creates a copy of the settings with credentials replaced by <see cref="SecretMask"/>.</summary>
    /// <remarks>
    /// The Jira token, Slack bot token, and Slack and Teams webhook URLs each grant access on their own, so they are
    /// never returned to the portal. The input is not changed, because it may be shared with the settings cache.
    /// </remarks>
    /// <param name="settings">Settings as stored.</param>
    /// <returns>A copy that is safe to return to the portal.</returns>
    public static DeploySettings RedactSecrets(DeploySettings settings)
    {
        return new DeploySettings
        {
            DeployWorkflow = settings.DeployWorkflow,
            Environments = settings.Environments,
            Services = settings.Services,
            DefaultEnvironment = settings.DefaultEnvironment,
            ReleaseEnvironment = settings.ReleaseEnvironment,
            PrdeployPortalUrl = settings.PrdeployPortalUrl,
            Badge = settings.Badge,
            Jira = settings.Jira == null ? null : new JiraSettings
            {
                AddIssuesEnabled = settings.Jira.AddIssuesEnabled,
                Host = settings.Jira.Host,
                Username = settings.Jira.Username,
                Password = Mask(settings.Jira.Password)
            },
            Slack = settings.Slack == null ? null : new SlackSettings
            {
                Token = Mask(settings.Slack.Token),
                EmailDomain = settings.Slack.EmailDomain,
                EmailAliases = settings.Slack.EmailAliases,
                NotificationsEnabled = settings.Slack.NotificationsEnabled,
                Webhooks = settings.Slack.Webhooks == null ? null : new SlackWebHooksSettings
                {
                    DeployUrl = Mask(settings.Slack.Webhooks.DeployUrl),
                    ReleaseUrl = Mask(settings.Slack.Webhooks.ReleaseUrl)
                }
            },
            Teams = settings.Teams == null ? null : new TeamsSettings
            {
                NotificationsEnabled = settings.Teams.NotificationsEnabled,
                Webhooks = settings.Teams.Webhooks == null ? null : new TeamsWebHooksSettings
                {
                    DeployUrl = Mask(settings.Teams.Webhooks.DeployUrl),
                    ReleaseUrl = Mask(settings.Teams.Webhooks.ReleaseUrl)
                }
            }
        };
    }

    /// <summary>Replaces any <see cref="SecretMask"/> values in settings being saved with the currently stored secrets.</summary>
    /// <param name="incoming">Settings received from the portal; updated in place.</param>
    /// <param name="stored">Settings currently stored, or <c>null</c> when there are none.</param>
    public static void RestoreSecrets(DeploySettings incoming, DeploySettings? stored)
    {
        if (incoming.Jira != null)
        {
            incoming.Jira.Password = Restore(incoming.Jira.Password, stored?.Jira?.Password);
        }

        if (incoming.Slack != null)
        {
            incoming.Slack.Token = Restore(incoming.Slack.Token, stored?.Slack?.Token);
            if (incoming.Slack.Webhooks != null)
            {
                incoming.Slack.Webhooks.DeployUrl = Restore(incoming.Slack.Webhooks.DeployUrl,
                    stored?.Slack?.Webhooks?.DeployUrl);
                incoming.Slack.Webhooks.ReleaseUrl = Restore(incoming.Slack.Webhooks.ReleaseUrl,
                    stored?.Slack?.Webhooks?.ReleaseUrl);
            }
        }

        if (incoming.Teams?.Webhooks != null)
        {
            incoming.Teams.Webhooks.DeployUrl = Restore(incoming.Teams.Webhooks.DeployUrl,
                stored?.Teams?.Webhooks?.DeployUrl);
            incoming.Teams.Webhooks.ReleaseUrl = Restore(incoming.Teams.Webhooks.ReleaseUrl,
                stored?.Teams?.Webhooks?.ReleaseUrl);
        }
    }

    /// <summary>Masks a secret value when one is configured.</summary>
    /// <param name="value">Secret value.</param>
    /// <returns><see cref="SecretMask"/> when a value is set; otherwise the unset value, so the portal can tell
    /// configured secrets from missing ones.</returns>
    private static string? Mask(string? value) => string.IsNullOrEmpty(value) ? value : SecretMask;

    /// <summary>Resolves an incoming secret value.</summary>
    /// <param name="incoming">Value received from the portal.</param>
    /// <param name="stored">Value currently stored.</param>
    /// <returns>The stored value when the incoming value is the mask; otherwise the incoming value.</returns>
    private static string? Restore(string? incoming, string? stored) => incoming == SecretMask ? stored : incoming;
}

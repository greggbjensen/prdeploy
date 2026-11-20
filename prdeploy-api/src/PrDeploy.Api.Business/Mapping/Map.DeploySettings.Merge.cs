using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Business.Mapping;
/// <summary>
/// Represents map data used by the application.
/// </summary>
public static partial class Map
{
    /// <summary>
    /// Performs the merge operation.
    /// </summary>
    /// <param name="target">Target environment or value for the operation.</param>
    /// <param name="source">Source environment or value for the operation.</param>
    public static void Merge(DeploySettings? target, DeploySettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.DeployWorkflow ??= source.DeployWorkflow;

        // We do not try to merge lists, they override.
        if (target.Environments?.Count == 0)
        {
            target.Environments = source.Environments;
        }

        if (target.Services?.Count == 0)
        {
            target.Services = source.Services;
        }

        target.DefaultEnvironment ??= source.DefaultEnvironment;
        target.ReleaseEnvironment ??= source.ReleaseEnvironment;

        if (target.Builds == null)
        {
            target.Builds = source.Builds;
        }
        else
        {
            Merge(target.Builds, source.Builds); 
        }

        if (target.Jira == null)
        {
            target.Jira = source.Jira;
        }
        else
        {
            Merge(target.Jira, source.Jira);
        }

        if (target.Slack == null)
        {
            target.Slack = source.Slack;
        }
        else
        {
            Merge(target.Slack, source.Slack);
        }

        if (target.Teams == null)
        {
            target.Teams = source.Teams;
        }
        else
        {
            Merge(target.Teams, source.Teams);
        }

        target.PrdeployPortalUrl ??= source.PrdeployPortalUrl;

        if (target.Badge == null)
        {
            target.Badge = source.Badge;
        }
        else
        {
            Merge(target.Badge, source.Badge);
        }
    }

    /// <summary>
    /// Performs the merge operation.
    /// </summary>
    /// <param name="target">Target environment or value for the operation.</param>
    /// <param name="source">Source environment or value for the operation.</param>
    private static void Merge(BuildsSettings? target, BuildsSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.CheckPattern ??= source.CheckPattern;
    }

    /// <summary>
    /// Performs the merge operation.
    /// </summary>
    /// <param name="target">Target environment or value for the operation.</param>
    /// <param name="source">Source environment or value for the operation.</param>
    private static void Merge(SlackSettings? target, SlackSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.Token ??= source.Token;
        target.EmailDomain ??= source.EmailDomain;
        target.EmailAliases ??= source.EmailAliases;
        target.NotificationsEnabled ??= source.NotificationsEnabled;

        if (target.Webhooks == null)
        {
            target.Webhooks = source.Webhooks;
        }
        else
        {
            Merge(target.Webhooks, source.Webhooks);
        }
    }

    /// <summary>
    /// Merges Microsoft Teams settings, keeping target values and filling gaps from the source.
    /// </summary>
    /// <param name="target">Target environment or value for the operation.</param>
    /// <param name="source">Source environment or value for the operation.</param>
    private static void Merge(TeamsSettings? target, TeamsSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.NotificationsEnabled ??= source.NotificationsEnabled;

        if (target.Webhooks == null)
        {
            target.Webhooks = source.Webhooks;
        }
        else
        {
            Merge(target.Webhooks, source.Webhooks);
        }
    }

    /// <summary>
    /// Merges Microsoft Teams web hook settings, keeping target values and filling gaps from the source.
    /// </summary>
    /// <param name="target">Target environment or value for the operation.</param>
    /// <param name="source">Source environment or value for the operation.</param>
    private static void Merge(TeamsWebHooksSettings? target, TeamsWebHooksSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.DeployUrl ??= source.DeployUrl;
        target.ReleaseUrl ??= source.ReleaseUrl;
    }

    /// <summary>
    /// Performs the merge operation.
    /// </summary>
    /// <param name="target">Target environment or value for the operation.</param>
    /// <param name="source">Source environment or value for the operation.</param>
    private static void Merge(SlackWebHooksSettings? target, SlackWebHooksSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.DeployUrl ??= source.DeployUrl;
        target.ReleaseUrl ??= source.ReleaseUrl;
    }

    /// <summary>
    /// Performs the merge operation.
    /// </summary>
    /// <param name="target">Target environment or value for the operation.</param>
    /// <param name="source">Source environment or value for the operation.</param>
    private static void Merge(JiraSettings? target, JiraSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.AddIssuesEnabled ??= source.AddIssuesEnabled;
        target.Host ??= source.Host;
        target.Username ??= source.Username;
        target.Password ??= source.Password;
    }

    /// <summary>
    /// Performs the merge operation.
    /// </summary>
    /// <param name="target">Target environment or value for the operation.</param>
    /// <param name="source">Source environment or value for the operation.</param>
    public static void Merge(Dictionary<string, object>? target, Dictionary<string, object>? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        foreach (var key in target.Keys)
        {
            if (!target.ContainsKey(key))
            {
                target[key] = source[key];
            }
        }
    }

    /// <summary>
    /// Performs the merge operation.
    /// </summary>
    /// <param name="target">Target environment or value for the operation.</param>
    /// <param name="source">Source environment or value for the operation.</param>
    public static void Merge(BadgeSettings? target, BadgeSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        Merge(target.StatusColors, source.StatusColors);
    }

    /// <summary>
    /// Performs the merge operation.
    /// </summary>
    /// <param name="target">Target environment or value for the operation.</param>
    /// <param name="source">Source environment or value for the operation.</param>
    public static void Merge(BadgeStatusColorsSettings? target, BadgeStatusColorsSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.Error ??= source.Error;
        target.Info ??= source.Info;
        target.Success ??= source.Success;
        target.Warn ??= source.Warn;
    }
}

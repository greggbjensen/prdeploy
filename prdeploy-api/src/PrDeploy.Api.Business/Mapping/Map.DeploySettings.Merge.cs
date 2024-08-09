using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Business.Mapping;
public static partial class Map
{
    public static void Merge(DeploySettings? target, DeploySettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.Owner ??= source.Owner;
        target.Repo ??= source.Repo;
        target.DeployWorkflow ??= source.DeployWorkflow;

        // We do not try to merge lists, they override.
        if (target.Environments?.Count == 0)
        {
            target.Environments = source.Environments;
        }

        target.DefaultEnvironment ??= source.DefaultEnvironment;
        target.ReleaseEnvironment ??= source.ReleaseEnvironment;
        target.SettingsBranch ??= source.SettingsBranch;
        target.DefaultBranch ??= source.DefaultBranch;

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

        target.DeployManagerSiteUrl ??= source.DeployManagerSiteUrl;

        if (target.Badge == null)
        {
            target.Badge = source.Badge;
        }
        else
        {
            Merge(target.Badge, source.Badge);
        }
    }

    private static void Merge(BuildsSettings? target, BuildsSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.CheckPattern ??= source.CheckPattern;
        target.WorkflowPattern ??= source.WorkflowPattern;
    }

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

    private static void Merge(SlackWebHooksSettings? target, SlackWebHooksSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.DeployUrl ??= source.DeployUrl;
        target.ReleaseUrl ??= source.ReleaseUrl;
    }

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

    public static void Merge(BadgeSettings? target, BadgeSettings? source)
    {
        if (source == null || target == null)
        {
            return;
        }

        Merge(target.StatusColors, source.StatusColors);
    }

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

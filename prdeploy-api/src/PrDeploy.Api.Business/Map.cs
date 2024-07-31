using PrDeploy.Api.Business.Models.Settings;
using PrDeploy.Api.Models;

namespace PrDeploy.Api.Business;
public static class Map
{
    public static PullRequest? PullRequest(Octokit.PullRequest? source) =>
        source != null
            ? new PullRequest
            {
                Number = source.Number,
                Url = source.HtmlUrl,
                Title = source.Title,
                Body = source.Body,
                User = Map.DeployUser(source.User),
                UpdatedAt = source.UpdatedAt
            }
            : null;

    public static PullRequest? PullRequest(Octokit.Issue? source) =>
        source != null
            ? new PullRequest
            {
                Number = source.Number,
                Url = source.HtmlUrl,
                Title = source.Title,
                Body = source.Body,
                User = Map.DeployUser(source.User),
                UpdatedAt = source.UpdatedAt
            }
            : null;

    public static DeployUser? DeployUser(Octokit.User? source) =>
        source != null
            ? new DeployUser
            {
                Name = source.Name ?? source.Login,
                Username = source.Login
            }
            : null;

    public static Repository? Repository(Octokit.Repository? source) =>
        source != null
            ? new Repository { Owner = source.Owner.Login, Repo = source.Name }
            : null;

    public static void Merge(RepoSettings? target, RepoSettings? source)
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
        target.AddJiraIssues ??= source.AddJiraIssues;

        if (target.Builds == null)
        {
            target.Builds = source.Builds;
        }
        else
        {
            Merge(target.Builds, source.Builds); 
        }

        if (target.Slack == null)
        {
            target.Slack = source.Slack;
        }
        else
        {
            Merge(target.Slack, source.Slack);
        }

        if (target.EmailAliases == null)
        {
            target.EmailAliases = source.EmailAliases;
        }
        else
        {
            Merge(target.EmailAliases, source.EmailAliases);
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
        target.NotificationsEnabled ??= source.NotificationsEnabled;

        if (target.Channels == null)
        {
            target.Channels = source.Channels;
        }
        else
        {
            Merge(target.Channels, source.Channels);
        }
    }

    public static void Merge(Dictionary<string, string>? target, Dictionary<string, string>? source)
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

using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Models.Settings.Compare;

namespace PrDeploy.Api.Business.Mapping;

public static partial class Map
{
    public static DeploySettingsCompare? Compare(DeploySettings? owner, DeploySettings? repo)
    {
        if (owner == null || repo == null)
        {
            return null;
        }

        var compare = new DeploySettingsCompare
        {
            DeployWorkflow = new()
            {
                Owner = owner.DeployWorkflow,
                Repo = repo.DeployWorkflow
            },
            DefaultEnvironment = new()
            {
                Owner = owner.DefaultEnvironment,
                Repo = repo.DefaultEnvironment
            },
            ReleaseEnvironment = new()
            {
                Owner = owner.ReleaseEnvironment,
                Repo = repo.ReleaseEnvironment
            },
            SettingsBranch = new()
            {
                Owner = owner.SettingsBranch,
                Repo = repo.SettingsBranch
            },
            DefaultBranch = new()
            {
                Owner = owner.DefaultBranch,
                Repo = repo.DefaultBranch
            },
            // We do not try to merge lists, they override.
            Environments = new()
            {
                Owner = owner.Environments,
                Repo = repo.Environments
            },
            // We do not try to merge lists, they override.
            Services = new ()
            {
                Owner = owner.Services,
                Repo = repo.Services
            },
            Builds = Compare(owner.Builds, repo.Builds),
            Jira = Compare(owner.Jira, repo.Jira),
            // TODO GBJ: Change to PrdeployPortalUrl.
            DeployManagerSiteUrl = new()
            {
                Owner = owner.DeployManagerSiteUrl,
                Repo = repo.DeployManagerSiteUrl
            },
            Slack = Compare(owner.Slack, repo.Slack),
            Badge = Compare(owner.Badge, repo.Badge)
        };

        return compare;
    }

    public static BuildsSettingsCompare Compare(BuildsSettings? owner, BuildsSettings? repo)
    {
        return new()
        {
            CheckPattern = new()
            {
                Owner = owner?.CheckPattern,
                Repo = repo?.CheckPattern
            },
            WorkflowPattern = new()
            {
                Owner = owner?.WorkflowPattern,
                Repo = repo?.WorkflowPattern
            }
        };
    }

    public static JiraSettingsCompare Compare(JiraSettings? owner, JiraSettings? repo)
    {
        return new()
        {
            AddIssuesEnabled = new()
            {
                Owner = owner?.AddIssuesEnabled,
                Repo = repo?.AddIssuesEnabled
            },
            Host = new()
            {
                Owner = owner?.Host,
                Repo = repo?.Host
            },
            Username = new()
            {
                Owner = owner?.Username,
                Repo = repo?.Username
            },
            Password = new()
            {
                Owner = owner?.Password,
                Repo = repo?.Password
            }
        };
    }

    public static SlackSettingsCompare Compare(SlackSettings? owner, SlackSettings? repo)
    {
        return new()
        {
            Token = new()
            {
                Owner = owner?.Token,
                Repo = repo?.Token
            },
            EmailDomain = new()
            {
                Owner = owner?.EmailDomain,
                Repo = repo?.EmailDomain
            },
            // Do not compare, just replace.
            EmailAliases = new()
            {
                Owner = owner?.EmailAliases,
                Repo = repo?.EmailAliases
            },
            NotificationsEnabled = new()
            {
                Owner = owner?.NotificationsEnabled,
                Repo = repo?.NotificationsEnabled
            },
            Webhooks = Compare(owner?.Webhooks, repo?.Webhooks)
        };
    }

    public static SlackWebHooksSettingsCompare Compare(SlackWebHooksSettings? owner, SlackWebHooksSettings? repo)
    {
        return new()
        {
            DeployUrl = new()
            {
                Owner = owner?.DeployUrl,
                Repo = repo?.DeployUrl
            },
            ReleaseUrl = new()
            {
                Owner = owner?.ReleaseUrl,
                Repo = repo?.ReleaseUrl
            }
        };
    }

    public static BadgeSettingsCompare Compare(BadgeSettings? owner, BadgeSettings? repo)
    {
        return new()
        {
            StatusColors = new ()
            {
                Error = new()
                {
                    Owner = owner?.StatusColors?.Error,
                    Repo = repo?.StatusColors?.Error
                },
                Info = new()
                {
                    Owner = owner?.StatusColors?.Info,
                    Repo = repo?.StatusColors?.Info
                },
                Success = new()
                {
                    Owner = owner?.StatusColors?.Success,
                    Repo = repo?.StatusColors?.Success
                },
                Warn = new()
                {
                    Owner = owner?.StatusColors?.Warn,
                    Repo = repo?.StatusColors?.Warn
                }
            }
        };
    }
}

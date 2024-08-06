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
            // TODO GBJ: Change to PrdeployPortalUrl.
            DeployManagerSiteUrl = new()
            {
                Owner = owner.DeployManagerSiteUrl,
                Repo = repo.DeployManagerSiteUrl
            },
            Builds = Compare(owner.Builds, repo.Builds)
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
}

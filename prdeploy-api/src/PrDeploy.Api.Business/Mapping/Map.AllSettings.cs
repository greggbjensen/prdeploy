using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Models.Settings.Compare;

namespace PrDeploy.Api.Business.Mapping;

public static partial class Map
{
    public static AllSettings? AllSettings(DeploySettings? owner, DeploySettings? repo)
    {
        if (owner == null || repo == null)
        {
            return null;
        }

        var compare = new DeploySettingsCompare();
        var allSettings = new AllSettings
        {
            DeploySettingsCompare = compare
        };

        compare.DeployWorkflow = new()
        {
            Owner = owner.DeployWorkflow,
            Repo = repo.DeployWorkflow
        };

        compare.DefaultEnvironment = new()
        {
            Owner = owner.DefaultEnvironment,
            Repo = repo.DefaultEnvironment
        };

        compare.ReleaseEnvironment = new()
        {
            Owner = owner.ReleaseEnvironment,
            Repo = repo.ReleaseEnvironment
        };

        compare.SettingsBranch = new()
        {
            Owner = owner.SettingsBranch,
            Repo = repo.SettingsBranch
        };

        compare.DefaultBranch = new()
        {
            Owner = owner.DefaultBranch,
            Repo = repo.DefaultBranch
        };

        // TODO GBJ: Change to PrdeployPortalUrl.
        compare.DeployManagerSiteUrl = new()
        {
            Owner = owner.DeployManagerSiteUrl,
            Repo = repo.DeployManagerSiteUrl
        };

        return allSettings;
    }
}

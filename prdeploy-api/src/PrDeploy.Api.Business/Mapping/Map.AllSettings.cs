using Amazon.Auth.AccessControlPolicy;
using Amazon.SimpleSystemsManagement.Model;
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

        // We do not try to merge lists, they override.
        compare.Environments = new()
        {
            Owner = owner.Environments,
            Repo = repo.Environments
        };

        // TODO GBJ: Change to PrdeployPortalUrl.
        compare.DeployManagerSiteUrl = new()
        {
            Owner = owner.DeployManagerSiteUrl,
            Repo = repo.DeployManagerSiteUrl
        };

        compare.Builds = Compare(owner.Builds, repo.Builds);

        return allSettings;
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

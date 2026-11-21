using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Tests.Security;

/// <summary>Verifies Microsoft Teams settings are compared and merged between owner and repo levels.</summary>
public class TeamsSettingsTest
{
    /// <summary>Confirms comparing keeps each level's webhook URLs and notifications flag.</summary>
    [Fact]
    public void Compare_TeamsSettings_KeepsOwnerAndRepoValues()
    {
        var owner = new DeploySettings
        {
            Teams = new TeamsSettings
            {
                NotificationsEnabled = true,
                Webhooks = new TeamsWebHooksSettings { DeployUrl = "https://owner/deploy", ReleaseUrl = "https://owner/release" }
            }
        };
        var repo = new DeploySettings
        {
            Teams = new TeamsSettings { Webhooks = new TeamsWebHooksSettings { DeployUrl = "https://repo/deploy" } }
        };

        var teams = Map.Compare(owner, repo)!.Teams;

        Assert.Equal("https://owner/deploy", teams.Webhooks.DeployUrl.Owner);
        Assert.Equal("https://repo/deploy", teams.Webhooks.DeployUrl.Repo);
        Assert.Equal("https://owner/release", teams.Webhooks.ReleaseUrl.Owner);
        Assert.Null(teams.Webhooks.ReleaseUrl.Repo);
        Assert.True(teams.NotificationsEnabled.Owner);
        Assert.Null(teams.NotificationsEnabled.Repo);
    }

    /// <summary>Confirms comparing settings without any Teams section still yields empty values.</summary>
    [Fact]
    public void Compare_NoTeamsSettings_ReturnsEmptyValues()
    {
        var teams = Map.Compare(new DeploySettings(), new DeploySettings())!.Teams;

        Assert.Null(teams.Webhooks.DeployUrl.Owner);
        Assert.Null(teams.NotificationsEnabled.Repo);
    }

    /// <summary>Confirms merging fills missing repo values from the owner and keeps repo overrides.</summary>
    [Fact]
    public void Merge_RepoOverridesOwner_FillsGapsFromOwner()
    {
        var target = new DeploySettings
        {
            Teams = new TeamsSettings
            {
                NotificationsEnabled = false,
                Webhooks = new TeamsWebHooksSettings { DeployUrl = "https://repo/deploy" }
            }
        };
        var source = new DeploySettings
        {
            Teams = new TeamsSettings
            {
                NotificationsEnabled = true,
                Webhooks = new TeamsWebHooksSettings { DeployUrl = "https://owner/deploy", ReleaseUrl = "https://owner/release" }
            }
        };

        Map.Merge(target, source);

        Assert.Equal("https://repo/deploy", target.Teams!.Webhooks!.DeployUrl);
        Assert.Equal("https://owner/release", target.Teams.Webhooks.ReleaseUrl);
        Assert.False(target.Teams.NotificationsEnabled);
    }

    /// <summary>Confirms merging adopts the owner's Teams section when the repo has none.</summary>
    [Fact]
    public void Merge_RepoWithoutTeams_UsesOwnerTeams()
    {
        var target = new DeploySettings();
        var source = new DeploySettings { Teams = new TeamsSettings { NotificationsEnabled = true } };

        Map.Merge(target, source);

        Assert.True(target.Teams!.NotificationsEnabled);
    }
}

using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Models.Settings;

namespace PrDeploy.Api.Tests.Security;

/// <summary>Verifies the settings mapping functions that mask credentials, restore them on save, and stop a repository that points Jira at a different host from inheriting the owner's credentials. The API behavior built on these is covered over HTTP in <see cref="ApiSecurityFunctionalTest"/>.</summary>
public class SettingsSecretsTest
{
    /// <summary>Confirms every credential is masked, non-secret values are kept, and the source settings are not changed.</summary>
    [Fact]
    public void RedactSecrets_MasksCredentialsWithoutChangingSource()
    {
        var source = WithSecrets("x");

        var redacted = Map.RedactSecrets(source);

        Assert.Equal(Map.SecretMask, redacted.Jira!.Password);
        Assert.Equal(Map.SecretMask, redacted.Slack!.Token);
        Assert.Equal(Map.SecretMask, redacted.Slack.Webhooks!.DeployUrl);
        Assert.Equal(Map.SecretMask, redacted.Slack.Webhooks.ReleaseUrl);
        Assert.Equal(Map.SecretMask, redacted.Teams!.Webhooks!.DeployUrl);
        Assert.Equal(Map.SecretMask, redacted.Teams.Webhooks.ReleaseUrl);
        Assert.Equal("https://x.atlassian.net", redacted.Jira.Host);
        Assert.Equal("x@myorg.com", redacted.Jira.Username);
        Assert.Equal("combined-deploy.yaml", redacted.DeployWorkflow);
        Assert.Equal("x-jira-token", source.Jira!.Password);
        Assert.Equal("xoxb-x", source.Slack!.Token);
    }

    /// <summary>Confirms unset secrets stay unset, so the portal can tell configured secrets from missing ones.</summary>
    [Fact]
    public void RedactSecrets_LeavesUnsetSecretsUnset()
    {
        var redacted = Map.RedactSecrets(new DeploySettings { Jira = new JiraSettings { Host = "https://j" } });

        Assert.Null(redacted.Jira!.Password);
        Assert.Null(redacted.Slack);
    }

    /// <summary>Confirms masked values are replaced by the stored secrets while new and cleared values are kept.</summary>
    [Fact]
    public void RestoreSecrets_KeepsStoredSecretsForMaskedValues()
    {
        var incoming = Map.RedactSecrets(WithSecrets("old"));
        incoming.Slack!.Token = "xoxb-new";
        incoming.Teams!.Webhooks!.ReleaseUrl = null;

        Map.RestoreSecrets(incoming, WithSecrets("old"));

        Assert.Equal("old-jira-token", incoming.Jira!.Password);
        Assert.Equal("xoxb-new", incoming.Slack.Token);
        Assert.Equal("https://hooks.slack.com/old/deploy", incoming.Slack.Webhooks!.DeployUrl);
        Assert.Null(incoming.Teams.Webhooks.ReleaseUrl);
    }

    /// <summary>Confirms a repository that overrides the Jira host does not inherit the owner's Jira credentials.</summary>
    [Fact]
    public void Merge_DifferentJiraHost_DoesNotInheritCredentials()
    {
        var repo = new DeploySettings { Jira = new JiraSettings { Host = "https://attacker.example.com" } };

        Map.Merge(repo, WithSecrets("owner"));

        Assert.Equal("https://attacker.example.com", repo.Jira!.Host);
        Assert.Null(repo.Jira.Username);
        Assert.Null(repo.Jira.Password);
    }

    /// <summary>Confirms credentials are inherited when the repository keeps the owner's Jira host.</summary>
    /// <param name="host">Repository host, or <c>null</c> to inherit it.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("https://OWNER.atlassian.net/")]
    public void Merge_SameJiraHost_InheritsCredentials(string? host)
    {
        var repo = new DeploySettings { Jira = new JiraSettings { Host = host, AddIssuesEnabled = true } };

        Map.Merge(repo, WithSecrets("owner"));

        Assert.Equal("owner@myorg.com", repo.Jira!.Username);
        Assert.Equal("owner-jira-token", repo.Jira.Password);
    }

    /// <summary>Creates settings with every kind of credential populated.</summary>
    /// <param name="prefix">Prefix that makes the values unique to one settings level.</param>
    /// <returns>The populated settings.</returns>
    private static DeploySettings WithSecrets(string prefix) => new()
    {
        DeployWorkflow = "combined-deploy.yaml",
        Environments = [],
        Jira = new JiraSettings
        {
            Host = $"https://{prefix}.atlassian.net",
            Username = $"{prefix}@myorg.com",
            Password = $"{prefix}-jira-token"
        },
        Slack = new SlackSettings
        {
            Token = $"xoxb-{prefix}",
            Webhooks = new SlackWebHooksSettings
            {
                DeployUrl = $"https://hooks.slack.com/{prefix}/deploy",
                ReleaseUrl = $"https://hooks.slack.com/{prefix}/release"
            }
        },
        Teams = new TeamsSettings
        {
            Webhooks = new TeamsWebHooksSettings
            {
                DeployUrl = $"https://teams.example.com/{prefix}/deploy",
                ReleaseUrl = $"https://teams.example.com/{prefix}/release"
            }
        }
    };
}

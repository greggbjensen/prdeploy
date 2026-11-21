using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Webhooks.Framework;

namespace PrDeploy.Api.Tests.Schema.Queries;

/// <summary>Contains tests that verify deploy settings GraphQL query and mutation Any-scalar coercion behavior.</summary>
[Collection(ApiHostCollection.Name)]
public class DeploySettingsQueryTest : DeployApiTest
{
    /// <summary>Initializes the test fixture and prepares its dependencies.</summary>
    /// <param name="factory">Object containing the factory data used by the operation.</param>
    public DeploySettingsQueryTest(DeployApiApplicationFactory factory)
        : base(factory)
    {
    }

    /// <summary>Verifies that querying repoSettings returns Any-typed Dictionary&lt;string, object&gt; fields without coercion errors.</summary>
    [Fact]
    public async Task RepoSettings_ReturnsAnyDictionaryFields()
    {
        var store = Factory.Services.GetRequiredService<IDeploymentStore>();
        var settings = WebhookTestHost.CreateDefaultSettings();
        settings.Slack ??= new SlackSettings();
        settings.Slack.EmailAliases = new Dictionary<string, object>
        {
            ["alice@example.com"] = "alice.alias@example.com",
            ["nested"] = new Dictionary<string, object>
            {
                ["count"] = 2,
                ["enabled"] = true
            }
        };
        var prodSettings = settings.Environments!.First(e => e.Name == "prod");
        prodSettings.AutomationTest = new AutomationTestSettings
        {
            Enabled = true,
            Workflow = "test-automation-run.yml",
            Inputs = new Dictionary<string, object>
            {
                ["environment"] = "${environment}"
            }
        };
        await store.SetAsync(WebhookTestHost.Owner, WebhookTestHost.Repo, "DEPLOY_SETTINGS", settings);

        using var httpClient = Factory.CreateClient();
        var response = await httpClient.PostAsJsonAsync("/graphql", new
        {
            query = """
                query ($input: RepoQueryInput!) {
                  repoSettings(input: $input) {
                    slack {
                      emailAliases
                    }
                    environments {
                      name
                      automationTest {
                        inputs
                      }
                    }
                  }
                }
                """,
            variables = new
            {
                input = new
                {
                    owner = WebhookTestHost.Owner,
                    repo = WebhookTestHost.Repo
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;

        Assert.False(
            root.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0,
            root.ToString());

        var repoSettings = root.GetProperty("data").GetProperty("repoSettings");
        var emailAliases = repoSettings.GetProperty("slack").GetProperty("emailAliases");
        Assert.Equal("alice.alias@example.com", emailAliases.GetProperty("alice@example.com").GetString());
        Assert.Equal("2", emailAliases.GetProperty("nested").GetProperty("count").GetString());
        Assert.Equal("true", emailAliases.GetProperty("nested").GetProperty("enabled").GetString());

        var prodEnv = repoSettings.GetProperty("environments")
            .EnumerateArray()
            .First(e => e.GetProperty("name").GetString() == "prod");
        var inputs = prodEnv.GetProperty("automationTest").GetProperty("inputs");
        Assert.Equal("${environment}", inputs.GetProperty("environment").GetString());
    }

    /// <summary>Verifies that querying deploySettingsCompare returns OwnerRepoDictionary Any fields and handles null or empty dictionaries.</summary>
    [Fact]
    public async Task DeploySettingsCompare_ReturnsOwnerRepoDictionaryAnyFields()
    {
        var store = Factory.Services.GetRequiredService<IDeploymentStore>();

        var ownerSettings = WebhookTestHost.CreateDefaultSettings();
        ownerSettings.Slack ??= new SlackSettings();
        ownerSettings.Slack.EmailAliases = new Dictionary<string, object>();
        await store.SetAsync(WebhookTestHost.Owner, "DEPLOY_SETTINGS", ownerSettings);

        var repoSettings = WebhookTestHost.CreateDefaultSettings();
        repoSettings.Slack ??= new SlackSettings();
        repoSettings.Slack.EmailAliases = new Dictionary<string, object>
        {
            ["bob@example.com"] = "bob.alias@example.com"
        };
        await store.SetAsync(WebhookTestHost.Owner, WebhookTestHost.Repo, "DEPLOY_SETTINGS", repoSettings);

        using var httpClient = Factory.CreateClient();
        var response = await httpClient.PostAsJsonAsync("/graphql", new
        {
            query = """
                query ($input: RepoQueryInput!) {
                  deploySettingsCompare(input: $input) {
                    slack {
                      emailAliases {
                        owner
                        repo
                      }
                    }
                  }
                }
                """,
            variables = new
            {
                input = new
                {
                    owner = WebhookTestHost.Owner,
                    repo = WebhookTestHost.Repo
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;

        Assert.False(
            root.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0,
            root.ToString());

        var emailAliases = root.GetProperty("data")
            .GetProperty("deploySettingsCompare")
            .GetProperty("slack")
            .GetProperty("emailAliases");
        Assert.Equal(JsonValueKind.Null, emailAliases.GetProperty("owner").ValueKind);
        Assert.Equal("bob.alias@example.com", emailAliases.GetProperty("repo").GetProperty("bob@example.com").GetString());
    }

    /// <summary>Verifies that repoSettingsSet accepts Any-typed Dictionary&lt;string, object&gt; inputs without coercion errors.</summary>
    [Fact]
    public async Task RepoSettingsSet_AcceptsAnyDictionaryInputs()
    {
        using var httpClient = Factory.CreateClient();
        var response = await httpClient.PostAsJsonAsync("/graphql", new
        {
            query = """
                mutation ($input: SetRepoSettingsInput!) {
                  repoSettingsSet(input: $input) {
                    success
                  }
                }
                """,
            variables = new
            {
                input = new
                {
                    owner = WebhookTestHost.Owner,
                    repo = WebhookTestHost.Repo,
                    settings = new
                    {
                        slack = new
                        {
                            emailAliases = new Dictionary<string, object>
                            {
                                ["carol@example.com"] = "carol.alias@example.com"
                            }
                        }
                    }
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;

        Assert.False(
            root.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0,
            root.ToString());
        Assert.True(root.GetProperty("data").GetProperty("repoSettingsSet").GetProperty("success").GetBoolean());

        var store = Factory.Services.GetRequiredService<IDeploymentStore>();
        await store.SetAsync(
            WebhookTestHost.Owner,
            WebhookTestHost.Repo,
            "DEPLOY_SETTINGS",
            WebhookTestHost.CreateDefaultSettings());
    }
}

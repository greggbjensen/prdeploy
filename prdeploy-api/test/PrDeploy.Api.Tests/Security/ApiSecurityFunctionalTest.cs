using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Mapping;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Tests.Framework;
using PrDeploy.Api.Tests.Webhooks.Framework;
using PrDeploy.Api.Webhooks.Status;

namespace PrDeploy.Api.Tests.Security;

/// <summary>Exercises authorization, input validation, and secret handling through the GraphQL HTTP endpoint with the real access checks. Only the GitHub API is mocked.</summary>
[Collection(ApiHostCollection.Name)]
public class ApiSecurityFunctionalTest : IClassFixture<SecuredApiApplicationFactory>
{
    private const string Owner = WebhookTestHost.Owner;
    private const string Repo = WebhookTestHost.Repo;
    private const string LockedRepo = "locked-repo";
    private const string SettingsKey = "DEPLOY_SETTINGS";

    private readonly SecuredApiApplicationFactory _factory;
    private readonly Mock<IGitHubClient> _github;
    private readonly IDeploymentStore _store;
    private readonly List<(string Repo, long Number, string Body)> _comments = [];

    /// <summary>Resets stored settings, cached access grants, and the GitHub mock so each test starts from the same state: the user is an active member of the owner organization, can push to the test repository, and can only read <see cref="LockedRepo"/>.</summary>
    /// <param name="factory">Shared API host with real access checks.</param>
    public ApiSecurityFunctionalTest(SecuredApiApplicationFactory factory)
    {
        _factory = factory;
        _github = factory.GitHub;
        _github.Reset();
        _store = factory.Services.GetRequiredService<IDeploymentStore>();
        ((MemoryCache)factory.Services.GetRequiredService<IMemoryCache>()).Compact(1.0);

        var settings = WebhookTestHost.CreateDefaultSettings();
        _store.SetAsync(Owner, SettingsKey, settings).GetAwaiter().GetResult();
        _store.SetAsync(Owner, Repo, SettingsKey, WebhookTestHost.CreateDefaultSettings()).GetAwaiter().GetResult();

        _github.Setup(g => g.Organization.Member.GetOrganizationMembership(Owner, SecuredApiApplicationFactory.Login))
            .ReturnsAsync(Membership(MembershipState.Active));
        _github.Setup(g => g.Repository.Get(Owner, Repo)).ReturnsAsync(RepositoryWith(push: true));
        _github.Setup(g => g.Repository.Get(Owner, LockedRepo)).ReturnsAsync(RepositoryWith(push: false));
        _github.Setup(g => g.Issue.Comment.Create(Owner, It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>()))
            .Callback<string, string, long, string>((_, repo, number, body) => _comments.Add((repo, number, body)))
            .ReturnsAsync((IssueComment)null!);
    }

    /// <summary>Confirms repository and owner settings queries return credentials masked and other values unchanged.</summary>
    [Fact]
    public async Task SettingsQueries_MaskCredentials()
    {
        using var result = await PostAsync("""
            query ($repo: RepoQueryInput!, $owner: OwnerQueryInput!) {
              repoSettings(input: $repo) { jira { host username password } slack { token webhooks { deployUrl releaseUrl } } }
              ownerSettings(input: $owner) { jira { password } slack { token } }
            }
            """, new { repo = new { owner = Owner, repo = Repo }, owner = new { owner = Owner } });

        var data = Data(result);
        var repoJira = data.GetProperty("repoSettings").GetProperty("jira");
        Assert.Equal("https://myorg.atlassian.net", repoJira.GetProperty("host").GetString());
        Assert.Equal("jira@myorg.com", repoJira.GetProperty("username").GetString());
        Assert.Equal(Map.SecretMask, repoJira.GetProperty("password").GetString());
        var repoSlack = data.GetProperty("repoSettings").GetProperty("slack");
        Assert.Equal(Map.SecretMask, repoSlack.GetProperty("token").GetString());
        Assert.Equal(Map.SecretMask, repoSlack.GetProperty("webhooks").GetProperty("deployUrl").GetString());
        Assert.Equal(Map.SecretMask, data.GetProperty("ownerSettings").GetProperty("jira").GetProperty("password").GetString());
        Assert.DoesNotContain("xoxb-test", result.RootElement.GetRawText());
    }

    /// <summary>Confirms the settings comparison the portal edits masks owner credentials, which outside collaborators with only repository access can read.</summary>
    [Fact]
    public async Task DeploySettingsCompare_MasksOwnerCredentials()
    {
        using var result = await PostAsync("""
            query ($input: RepoQueryInput!) {
              deploySettingsCompare(input: $input) { jira { password { owner repo } } slack { token { owner repo } } }
            }
            """, new { input = new { owner = Owner, repo = Repo } });

        var compare = Data(result).GetProperty("deploySettingsCompare");
        Assert.Equal(Map.SecretMask, compare.GetProperty("jira").GetProperty("password").GetProperty("owner").GetString());
        Assert.Equal(Map.SecretMask, compare.GetProperty("slack").GetProperty("token").GetProperty("owner").GetString());
        Assert.Equal(Map.SecretMask, compare.GetProperty("jira").GetProperty("password").GetProperty("repo").GetString());
    }

    /// <summary>Confirms saving the masked values the portal received keeps the stored secrets, while a new value replaces them.</summary>
    [Fact]
    public async Task RepoSettingsSet_KeepsStoredSecretsForMaskedValues()
    {
        using var result = await PostAsync("""
            mutation ($input: SetRepoSettingsInput!) { repoSettingsSet(input: $input) { success } }
            """, new
        {
            input = new
            {
                owner = Owner,
                repo = Repo,
                settings = new
                {
                    jira = new { host = "https://myorg.atlassian.net", username = "jira@myorg.com", password = Map.SecretMask },
                    slack = new { token = "xoxb-new" }
                }
            }
        });

        Assert.True(Data(result).GetProperty("repoSettingsSet").GetProperty("success").GetBoolean());
        var stored = await _store.GetAsync<DeploySettings>(Owner, Repo, SettingsKey);
        Assert.Equal("token", stored.Jira!.Password);
        Assert.Equal("xoxb-new", stored.Slack!.Token);
    }

    /// <summary>Confirms a user who can only read a repository cannot read its settings, list its pull requests, change its queues, or post deploy commands to it.</summary>
    /// <param name="operation">GraphQL operation to send.</param>
    [Theory]
    [InlineData("""query ($r: RepoQueryInput!) { repoSettings(input: $r) { deployWorkflow } }""")]
    [InlineData("""query ($r: RepoQueryInput!) { deploySettingsCompare(input: $r) { deployWorkflow { repo } } }""")]
    [InlineData("""query ($r: RepoQueryInput!) { deployQueues(input: $r) { environment } }""")]
    [InlineData("""query { openPullRequests(input: { owner: "greggbjensen", repo: "locked-repo" }) { number } }""")]
    [InlineData("""mutation { deployQueueUpdate(input: { owner: "greggbjensen", repo: "locked-repo", environment: "dev", pullNumbers: [1] }) { environment } }""")]
    [InlineData("""mutation { deployEnvironmentDeploy(input: { owner: "greggbjensen", repo: "locked-repo", environment: "prod", pullNumber: 1, force: true }) { success } }""")]
    [InlineData("""mutation { pullRequestAddServices(input: { owner: "greggbjensen", repo: "locked-repo", pullNumber: 1, services: ["api"] }) { success } }""")]
    public async Task ReadOnlyRepository_IsForbidden(string operation)
    {
        using var result = await PostAsync(operation, new { r = new { owner = Owner, repo = LockedRepo } });

        Assert.Equal("FORBIDDEN", ErrorCode(result));
        Assert.Empty(_comments);
        Assert.Null(await _store.GetAsync<List<int>?>(Owner, LockedRepo, "DEV_PR_QUEUE"));
    }

    /// <summary>Confirms names that are not valid on GitHub, which could change storage keys or API paths, and owners that are not allowed are denied before GitHub is asked about them.</summary>
    /// <param name="owner">Owner to request.</param>
    /// <param name="repo">Repository to request.</param>
    [Theory]
    [InlineData(Owner, "prdeploy-example-repo/../other")]
    [InlineData(Owner, "..")]
    [InlineData("greggbjensen/x", "r")]
    [InlineData("someone-else", "r")]
    public async Task InvalidOrDisallowedOwnerRepo_IsForbiddenWithoutLookup(string owner, string repo)
    {
        using var result = await PostAsync("""
            query ($r: RepoQueryInput!) { repoSettings(input: $r) { deployWorkflow } }
            """, new { r = new { owner, repo } });

        Assert.Equal("FORBIDDEN", ErrorCode(result));
        _github.Verify(g => g.Repository.Get(owner, repo), Times.Never);
        _github.Verify(g => g.Organization.Member.GetOrganizationMembership(owner, It.IsAny<string>()), Times.Never);
    }

    /// <summary>Confirms environment and service values cannot add lines or other commands to the comment posted for the user.</summary>
    /// <param name="operation">GraphQL mutation with an injected value.</param>
    [Theory]
    [InlineData("""mutation { deployEnvironmentDeploy(input: { owner: "greggbjensen", repo: "prdeploy-example-repo", environment: "dev\n/deploy prod --force", pullNumber: 8 }) { success } }""")]
    [InlineData("""mutation { deployEnvironmentFree(input: { owner: "greggbjensen", repo: "prdeploy-example-repo", environment: "dev; /rollback prod", pullNumber: 8 }) { success } }""")]
    [InlineData("""mutation { pullRequestAddServices(input: { owner: "greggbjensen", repo: "prdeploy-example-repo", pullNumber: 8, services: ["api\n/deploy prod"] }) { success } }""")]
    public async Task CommandInjection_IsRejected(string operation)
    {
        using var result = await PostAsync(operation, new { });

        Assert.Equal("BAD_REQUEST", ErrorCode(result));
        Assert.Empty(_comments);
    }

    /// <summary>Confirms an authorized deploy posts exactly one single-line command as the user.</summary>
    [Fact]
    public async Task DeployEnvironmentDeploy_PostsCommandForWriter()
    {
        using var result = await PostAsync("""
            mutation { deployEnvironmentDeploy(input: { owner: "greggbjensen", repo: "prdeploy-example-repo", environment: "Stage", pullNumber: 8, force: true, retain: true }) { success } }
            """, new { });

        Assert.True(Data(result).GetProperty("deployEnvironmentDeploy").GetProperty("success").GetBoolean());
        var (repo, number, body) = Assert.Single(_comments);
        Assert.Equal((Repo, 8L), (repo, number));
        Assert.Equal("/deploy stage --force --retain", body.Trim());
    }

    /// <summary>Confirms listing queues does not rename environments in the cached merged settings.</summary>
    [Fact]
    public async Task DeployQueues_DoesNotChangeEnvironmentNames()
    {
        var settings = WebhookTestHost.CreateDefaultSettings();
        settings.Environments = [new EnvironmentSettings { Name = "dev1", Queue = "DEV_PR_QUEUE", Color = "#000000" }];
        await _store.SetAsync(Owner, Repo, SettingsKey, settings);

        using var queues = await PostAsync("""
            query ($r: RepoQueryInput!) { deployQueues(input: $r) { environment } }
            """, new { r = new { owner = Owner, repo = Repo } });
        using var environments = await PostAsync("""
            query ($r: RepoQueryInput!) { environments(input: $r) { name } }
            """, new { r = new { owner = Owner, repo = Repo } });

        Assert.Equal("dev", Data(queues).GetProperty("deployQueues")[0].GetProperty("environment").GetString());
        Assert.Equal("dev1", Data(environments).GetProperty("environments")[0].GetProperty("name").GetString());
    }

    /// <summary>Confirms the deploy status stream sends the current status as a deploy-status event to a user with access.</summary>
    [Fact]
    public async Task DeployStatusEvents_StreamsCurrentStatusToUserWithAccess()
    {
        _factory.Services.GetRequiredService<DeployStatusBroadcaster>().Publish(new DeployWorkflowStatus(
            Owner, Repo, "sse-dev", DeployWorkflowState.Successful, 12, 34, null, DateTimeOffset.UnixEpoch));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync($"/api/deploy-status/{Owner}/{Repo}/events",
            HttpCompletionOption.ResponseHeadersRead, cts.Token);
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cts.Token));
        var eventLine = await reader.ReadLineAsync(cts.Token);
        var dataLine = await reader.ReadLineAsync(cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("event: deploy-status", eventLine);
        using var data = JsonDocument.Parse(dataLine!["data: ".Length..]);
        Assert.Equal("sse-dev", data.RootElement.GetProperty("environment").GetString());
        Assert.Equal("Successful", data.RootElement.GetProperty("state").GetString());
        Assert.Equal(12, data.RootElement.GetProperty("pullNumber").GetInt32());
    }

    /// <summary>Confirms the deploy status stream refuses a repository the user cannot push to.</summary>
    [Fact]
    public async Task DeployStatusEvents_ForbidsUserWithoutRepoAccess()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync($"/api/deploy-status/{Owner}/{LockedRepo}/events");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Sends a GraphQL operation to the API over HTTP.</summary>
    /// <param name="query">Operation text.</param>
    /// <param name="variables">Operation variables.</param>
    /// <returns>The parsed response body.</returns>
    private async Task<JsonDocument> PostAsync(string query, object variables)
    {
        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/graphql", new { query, variables });
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    /// <summary>Returns the response data, failing with the errors when there are any.</summary>
    /// <param name="result">Parsed GraphQL response.</param>
    /// <returns>The <c>data</c> element.</returns>
    private static JsonElement Data(JsonDocument result)
    {
        Assert.False(result.RootElement.TryGetProperty("errors", out var errors), errors.ToString());
        return result.RootElement.GetProperty("data");
    }

    /// <summary>Returns the code of the first GraphQL error.</summary>
    /// <param name="result">Parsed GraphQL response.</param>
    /// <returns>The error code, or <c>null</c> when the response has no errors.</returns>
    private static string? ErrorCode(JsonDocument result) =>
        result.RootElement.TryGetProperty("errors", out var errors)
            ? errors[0].GetProperty("extensions").GetProperty("code").GetString()
            : null;

    /// <summary>Creates a repository the user has the given push permission on.</summary>
    /// <param name="push">Whether the user can push.</param>
    /// <returns>The repository GitHub returns.</returns>
    private static Repository RepositoryWith(bool push)
    {
        var repository = new Repository();
        typeof(Repository).GetProperty(nameof(Repository.Permissions))!
            .SetValue(repository, new RepositoryPermissions(false, false, push, false, true));
        return repository;
    }

    /// <summary>Creates an organization membership in the given state.</summary>
    /// <param name="state">Membership state.</param>
    /// <returns>The membership GitHub returns.</returns>
    private static OrganizationMembership Membership(MembershipState state)
    {
        var membership = new OrganizationMembership();
        typeof(OrganizationMembership).GetProperty(nameof(OrganizationMembership.State))!
            .SetValue(membership, new StringEnum<MembershipState>(state));
        return membership;
    }
}

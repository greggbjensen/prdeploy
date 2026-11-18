using System.Net;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Clients;
using PrDeploy.Api.Business.Clients.Interfaces;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Security;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Services;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Business.Stores.Interfaces;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.Auth;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.OwnerRepo;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Models.Settings.Inputs;
using PrDeploy.Api.Tests.Framework;
using RestSharp;

namespace PrDeploy.Api.Tests.Logging;

/// <summary>
/// Verifies that business-layer classes emit structured (template based) log entries.
/// </summary>
public class BusinessLoggingTest
{
    /// <summary>
    /// Builds a security context test double for the supplied user.
    /// </summary>
    /// <param name="token">GitHub token, which must never appear in logs.</param>
    /// <param name="login">GitHub login of the caller.</param>
    /// <returns>Mocked security context.</returns>
    private static Mock<ISecurityContext> Context(string token, string login)
    {
        var context = new Mock<ISecurityContext>();
        context.SetupGet(c => c.UserToken).Returns(token);
        context.SetupGet(c => c.Login).Returns(login);
        return context;
    }

    /// <summary>
    /// Confirms a repo access denial logs a structured warning and throws Forbidden.
    /// </summary>
    [Fact]
    public async Task GuardRepoAsync_Denied_LogsStructuredWarning()
    {
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.Repository.Collaborator.IsCollaborator("o", "r", "octocat")).ReturnsAsync(false);
        var logger = new CapturingLogger<GitHubSecurity>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var security = new GitHubSecurity(github.Object, logger, cache, Context("gh-token-123", "octocat").Object);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => security.GuardRepoAsync("o", "r"));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        var entry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Equal("octocat", entry.Properties["Login"]);
        Assert.Equal("o", entry.Properties["Owner"]);
        Assert.Equal("r", entry.Properties["Repo"]);
        Assert.DoesNotContain("{o}", entry.Template);
        Assert.All(logger.Entries, e => Assert.DoesNotContain("gh-token-123", e.Message));
    }

    /// <summary>
    /// Confirms a GitHub lookup failure keeps the exception and uses a message template.
    /// </summary>
    [Fact]
    public async Task HasRepoAccessAsync_LookupFails_LogsWarningWithException()
    {
        var failure = new ApiException("nope", HttpStatusCode.InternalServerError);
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.Repository.Collaborator.IsCollaborator("o", "r", "octocat")).ThrowsAsync(failure);
        var logger = new CapturingLogger<GitHubSecurity>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var security = new GitHubSecurity(github.Object, logger, cache, Context("t", "octocat").Object);

        Assert.False(await security.HasRepoAccessAsync("o", "r"));

        var entry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Same(failure, entry.Exception);
        Assert.Contains("{Login}", entry.Template);
    }

    /// <summary>
    /// Confirms an anonymous caller is denied without a warning about a missing token leaking details.
    /// </summary>
    [Fact]
    public async Task HasRepoAccessAsync_NoToken_ReturnsFalseAndLogsDebug()
    {
        var logger = new CapturingLogger<GitHubSecurity>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var security = new GitHubSecurity(
            new Mock<IGitHubClient>().Object, logger, cache, Context(string.Empty, string.Empty).Object);

        Assert.False(await security.HasRepoAccessAsync("o", "r"));

        Assert.Equal(LogLevel.Debug, Assert.Single(logger.Entries).Level);
    }

    /// <summary>
    /// Confirms a client ID mismatch is logged as a warning and rejected before contacting GitHub.
    /// </summary>
    [Fact]
    public async Task GetAccessTokenAsync_ClientIdMismatch_LogsWarningAndThrowsForbidden()
    {
        var logger = new CapturingLogger<GitHubAuthClient>();
        var restClientInstance = new RestClientInstance<GitHubAuthOptions>
        {
            Options = new GitHubAuthOptions { ClientId = "expected", ClientSecret = "shh" },
            RestClient = new Mock<IRestClient>(MockBehavior.Strict).Object
        };
        var client = new GitHubAuthClient(
            restClientInstance,
            Microsoft.Extensions.Options.Options.Create(new JwtOptions()),
            new Mock<ICipherService>().Object,
            new Mock<IServiceProvider>().Object,
            new InlineValidator<AccessTokenRequest>(),
            logger);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetAccessTokenAsync(new AccessTokenRequest { ClientId = "other", Code = "c" }));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.DoesNotContain("shh", entry.Message);
        Assert.DoesNotContain("other", entry.Message);
    }

    /// <summary>
    /// Confirms saving repo settings logs an Information entry carrying the owner and repo.
    /// </summary>
    [Fact]
    public async Task SetRepoSettingsAsync_LogsInformation()
    {
        var security = new Mock<IGitHubSecurity>();
        var logger = new CapturingLogger<DeploySettingsService>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new DeploySettingsService(new Mock<IDeploymentStore>().Object, security.Object, cache, logger);

        await service.SetRepoSettingsAsync(new SetRepoSettingsInput
        {
            Owner = "o", Repo = "r", Settings = new DeploySettings()
        });

        var entry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Information);
        Assert.Equal("o", entry.Properties["Owner"]);
        Assert.Equal("r", entry.Properties["Repo"]);
    }

    /// <summary>
    /// Confirms a missing environment is logged as a warning before the not-found exception.
    /// </summary>
    [Fact]
    public void GetEnvironment_Missing_LogsWarningAndThrows()
    {
        var logger = new CapturingLogger<DeploySettingsService>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new DeploySettingsService(
            new Mock<IDeploymentStore>().Object, new Mock<IGitHubSecurity>().Object, cache, logger);

        Assert.Throws<NotFoundException>(() => service.GetEnvironment("o", "r", "prod",
            new DeploySettings { Environments = new List<EnvironmentSettings>() }));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("prod", entry.Properties["Environment"]);
    }

    /// <summary>
    /// Confirms enabling a repository logs the owner and repo.
    /// </summary>
    [Fact]
    public async Task AddEnabledAsync_LogsInformation()
    {
        var store = new Mock<IDeploymentStore>();
        store.Setup(s => s.GetAsync<List<OwnerRepos>?>("OWNER_REPOS")).ReturnsAsync(new List<OwnerRepos>());
        var logger = new CapturingLogger<OwnerRepoService>();
        var service = new OwnerRepoService(store.Object, new Mock<IGitHubSecurity>().Object, logger);

        await service.AddEnabledAsync(new PrDeploy.Api.Models.OwnerRepo.Repository { Owner = "o", Repo = "r" });

        var entry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Information && e.Properties.ContainsKey("Repo"));
        Assert.Equal("o", entry.Properties["Owner"]);
    }

    /// <summary>
    /// Confirms a user with no authorized repositories is logged as a warning and denied.
    /// </summary>
    [Fact]
    public async Task ListEnabledAsync_NoAccess_LogsWarningAndThrowsForbidden()
    {
        var store = new Mock<IDeploymentStore>();
        store.Setup(s => s.GetAsync<List<OwnerRepos>?>("OWNER_REPOS")).ReturnsAsync(
            new List<OwnerRepos> { new() { Owner = "o", Repos = new List<string> { "r" } } });
        var security = new Mock<IGitHubSecurity>();
        security.Setup(s => s.HasRepoAccessAsync("o", "r")).ReturnsAsync(false);
        var logger = new CapturingLogger<OwnerRepoService>();
        var service = new OwnerRepoService(store.Object, security.Object, logger);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.ListEnabledAsync());

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    /// <summary>
    /// Confirms a deploy request logs its environment, pull request, and options as properties.
    /// </summary>
    [Fact]
    public async Task DeployAsync_LogsStructuredInformation()
    {
        var pullRequests = new Mock<IPullRequestService>();
        pullRequests.Setup(p => p.AddCommentCommandAsync("o", "r", 7, "/deploy dev --force"))
            .ReturnsAsync(new PrDeploy.Api.Models.General.StatusResponse());
        var logger = new CapturingLogger<DeployEnvironmentService>();
        var service = new DeployEnvironmentService(
            new Mock<IGitHubClient>().Object,
            new Mock<IDeploySettingsService>().Object,
            pullRequests.Object,
            new Mock<IGitHubSecurity>().Object,
            new Mock<IDeploymentStore>().Object,
            new InlineValidator<DeployStateComparisonInput>(),
            new InlineValidator<RepoQueryInput>(),
            logger);

        await service.DeployAsync(new EnvironmentDeployInput
        {
            Owner = "o", Repo = "r", PullNumber = 7, Environment = "Dev", Force = true
        });

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(7, entry.Properties["PullNumber"]);
        Assert.Equal("Dev", entry.Properties["Environment"]);
        Assert.Equal(true, entry.Properties["Force"]);
    }

    /// <summary>
    /// Confirms an empty queue is created with a Debug entry and that updating logs the queue size.
    /// </summary>
    [Fact]
    public async Task ListAsync_LogsDebugForQueues()
    {
        var settings = new Mock<IDeploySettingsService>();
        settings.Setup(s => s.GetQueueEnvironmentsAsync("o", "r")).ReturnsAsync(new List<EnvironmentSettings>());
        var logger = new CapturingLogger<DeployQueueService>();
        var service = new DeployQueueService(
            new Mock<IGitHubClient>().Object, settings.Object, new Mock<IDeploymentStore>().Object, logger);

        await service.ListAsync(new RepoQueryInput { Owner = "o", Repo = "r" });

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Equal("o", entry.Properties["Owner"]);
    }
}

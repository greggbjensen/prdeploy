using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Octokit;
using PrDeploy.Api.Models.Settings;
using PrDeploy.Api.Webhooks;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Tests.Webhooks.Framework;

/// <summary>
/// Builds the webhook services for a single event with a mocked GitHub client, parameter store and HTTP calls.
/// </summary>
public sealed class WebhookTestHost : IDisposable
{
    public const string Owner = "greggbjensen";
    public const string Repo = "prdeploy-example-repo";
    public const string RepositoryUrl = $"https://github.com/{Owner}/{Repo}";

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public WebhookTestHost(DeploySettings? settings = null)
    {
        GitHub = new Mock<IGitHubClient> { DefaultValue = DefaultValue.Mock };

        var services = new ServiceCollection()
            .AddLogging(b => b.SetMinimumLevel(LogLevel.Debug))
            .AddPrDeployWebhookServices()
            .AddSingleton<PrDeploy.Api.Business.Stores.Interfaces.IParameterStore>(ParameterStore);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(Http));
        services.AddSingleton(httpClientFactory.Object);

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();

        Context = Get<WebhookContext>();
        Context.GitHub = GitHub.Object;
        Context.Owner = Owner;
        Context.Repo = Repo;
        Context.RepositoryHtmlUrl = RepositoryUrl;
        Context.DefaultBranch = "main";
        Context.Settings = settings ?? CreateDefaultSettings();
        WebhookSettingsService.NormalizeDefaults(Context.Settings);
    }

    public Mock<IGitHubClient> GitHub { get; }

    public InMemoryParameterStore ParameterStore { get; } = new();

    public FakeHttpMessageHandler Http { get; } = new();

    public WebhookContext Context { get; }

    public T Get<T>() where T : notnull => _scope.ServiceProvider.GetRequiredService<T>();

    public Mock<T> MockOf<T>(T client) where T : class => Mock.Get(client);

    /// <summary>
    /// Same environments as the original Node.js webhooks test settings.
    /// </summary>
    public static DeploySettings CreateDefaultSettings() => new()
    {
        DeployWorkflow = "deploy.yml",
        Environments =
        [
            Environment("dev", "DEV_PR_QUEUE", "#d4ac0d"),
            Environment("dev2", "DEV_PR_QUEUE", "#d4ac0d"),
            Environment("dev3", "DEV_PR_QUEUE", "#d4ac0d"),
            Environment("stage", "STAGE_PR_QUEUE", "#2e86c1"),
            Environment("prod", "PROD_PR_QUEUE", "#1d8348", requireApproval: true)
        ],
        Services =
        [
            new ServiceSettings { Name = "deploy-api-test", Path = "services/deploy-api-test" },
            new ServiceSettings { Name = "deploy-app-test" }
        ],
        DefaultEnvironment = "dev",
        ReleaseEnvironment = "prod",
        PrdeployPortalUrl = "https://prdeploy.myorg.com",
        Builds = new BuildsSettings
        {
            CheckPattern = "Build helm chart",
            WorkflowPattern = "/([^/]+?)-build.ya?ml"
        },
        Badge = new BadgeSettings
        {
            StatusColors = new BadgeStatusColorsSettings
            {
                Error = "ff0000",
                Warn = "cccc00",
                Success = "00ff00",
                Info = "0080ff"
            }
        },
        Slack = new SlackSettings
        {
            NotificationsEnabled = true,
            Token = "xoxb-test",
            EmailDomain = "myorg.com",
            EmailAliases = new Dictionary<string, object> { ["jdoe@myorg.com"] = "john.doe@myorg.com" },
            Webhooks = new SlackWebHooksSettings
            {
                DeployUrl = "https://hooks.slack.com/deploy",
                ReleaseUrl = "https://hooks.slack.com/release"
            }
        },
        Jira = new JiraSettings
        {
            AddIssuesEnabled = true,
            Host = "https://myorg.atlassian.net",
            Username = "jira@myorg.com",
            Password = "token"
        }
    };

    private static EnvironmentSettings Environment(string name, string queue, string color,
        bool requireApproval = false) => new()
    {
        Name = name,
        Queue = queue,
        Color = color,
        Url = $"https://{name}.myorg.com",
        RequireApproval = requireApproval
    };

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}

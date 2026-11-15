using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Webhooks.Auth;
using PrDeploy.Api.Webhooks.Events;
using PrDeploy.Api.Webhooks.Managers;
using PrDeploy.Api.Webhooks.Options;
using PrDeploy.Api.Webhooks.Services;
using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Webhooks;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds the GitHub App webhook endpoint dependencies and background event processing.
    /// </summary>
    public static IServiceCollection AddPrDeployWebhooks(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .Configure<GitHubAppOptions>(configuration.GetSection(GitHubAppOptions.SectionName))
            .AddSingleton<IGitHubAppClientFactory, GitHubAppClientFactory>()
            .AddSingleton<WebhookEventQueue>()
            .AddSingleton<WebhookRequestHandler>()
            .AddSingleton<WebhookEventDispatcher>()
            .AddHostedService<WebhookEventWorker>();

        return services.AddPrDeployWebhookServices();
    }

    /// <summary>
    /// Adds the services that process an event, each scoped to a single webhook event.
    /// </summary>
    public static IServiceCollection AddPrDeployWebhookServices(this IServiceCollection services)
    {
        services.AddHttpClient(SlackService.HttpClientName);
        services.AddHttpClient(JiraService.HttpClientName);
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        return services
            .AddScoped<WebhookContext>()
            .AddScoped<EnvironmentUtil>()
            .AddSingleton<TemplateService>()
            .AddScoped<WebhookSettingsService>()
            .AddScoped<DeployStateService>()
            .AddScoped<PullRequestService>()
            .AddScoped<SlackService>()
            .AddScoped<JiraService>()
            .AddScoped<CheckService>()
            .AddScoped<DeployService>()
            .AddScoped<StatusService>()
            .AddScoped<AutomationTestService>()
            .AddScoped<CommentCommandManager>()
            .AddScoped<JiraManager>()
            .AddScoped<StatusManager>();
    }

    /// <summary>
    /// Maps the endpoint configured as the GitHub App Webhook URL.
    /// </summary>
    public static IEndpointConventionBuilder MapGitHubWebhooks(this IEndpointRouteBuilder endpoints,
        string pattern = "/webhooks")
    {
        return endpoints
            .MapPost(pattern, (HttpContext context, WebhookRequestHandler handler) =>
                handler.HandleAsync(context.Request, context.RequestAborted))
            .AllowAnonymous(); // Authenticated by the webhook signature.
    }
}

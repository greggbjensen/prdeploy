using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PrDeploy.Api.Webhooks.Auth;
using PrDeploy.Api.Webhooks.Events;
using PrDeploy.Api.Webhooks.Managers;
using PrDeploy.Api.Webhooks.Options;
using PrDeploy.Api.Webhooks.Services;
using PrDeploy.Api.Webhooks.Status;
using PrDeploy.Api.Webhooks.Utils;

namespace PrDeploy.Api.Webhooks;

/// <summary>Defines the contract for service Collection Extensions operations.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Adds the pr deploy webhooks to the associated collection.</summary>
    /// <param name="services">Services to include in the operation.</param>
    /// <param name="configuration">Object containing the configuration data used by the operation.</param>
    /// <returns>The i service collection produced by the operation.</returns>
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

    /// <summary>Adds the pr deploy webhook services to the associated collection.</summary>
    /// <param name="services">Services to include in the operation.</param>
    /// <returns>The i service collection produced by the operation.</returns>
    public static IServiceCollection AddPrDeployWebhookServices(this IServiceCollection services)
    {
        services.AddHttpClient(SlackService.HttpClientName);
        services.AddHttpClient(JiraService.HttpClientName);
        services.AddMemoryCache();
        services.TryAddSingleton<DeployStatusBroadcaster>();
        services.AddSingleton(TimeProvider.System);

        return services
            .AddScoped<WebhookContext>()
            .AddScoped<EnvironmentUtil>()
            .AddSingleton<TemplateService>()
            .AddScoped<WebhookSettingsService>()
            .AddScoped<BuildNameService>()
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

    /// <summary>Maps the endpoint configured as the GitHub App Webhook URL.</summary>
    public static IEndpointConventionBuilder MapGitHubWebhooks(this IEndpointRouteBuilder endpoints,
        string pattern = "/webhooks")
    {
        return endpoints
            .MapPost(pattern, (HttpContext context, WebhookRequestHandler handler) =>
                handler.HandleAsync(context.Request, context.RequestAborted))
            .WithName("GitHubWebhook")
            .WithSummary("Receive a GitHub App webhook")
            .WithDescription("Validates the GitHub signature and queues supported events. Returns 202 when queued, or 200 when ignored.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .AllowAnonymous(); // Authenticated by the webhook signature.
    }
}

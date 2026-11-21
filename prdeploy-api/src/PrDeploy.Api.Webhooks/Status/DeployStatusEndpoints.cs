using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PrDeploy.Api.Business.Security.Interfaces;
using System.Net.ServerSentEvents;

namespace PrDeploy.Api.Webhooks.Status;

/// <summary>
/// Maps the Server-Sent Events stream of deployment workflow status used by the portal dashboard.
/// </summary>
public static class DeployStatusEndpoints
{
    /// <summary>
    /// Name of the SSE event carrying a <see cref="DeployWorkflowStatus"/>.
    /// </summary>
    public const string EventType = "deploy-status";

    /// <summary>
    /// Maps <c>GET /api/deploy-status/{owner}/{repo}/events</c>. The caller must be authenticated and have access to
    /// the repository.
    /// </summary>
    /// <param name="endpoints">Route builder the endpoint is added to.</param>
    /// <returns>The endpoint convention builder.</returns>
    public static IEndpointConventionBuilder MapDeployStatusEvents(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/api/deploy-status/{owner}/{repo}/events", async (string owner, string repo,
                IGitHubSecurity security, DeployStatusBroadcaster broadcaster, HttpContext context,
                CancellationToken cancellationToken) =>
            {
                if (!await security.HasRepoAccessAsync(owner, repo))
                {
                    return Results.Forbid();
                }

                // Stop nginx-style proxies and ingresses from buffering the stream.
                context.Response.Headers["X-Accel-Buffering"] = "no";
                return TypedResults.ServerSentEvents(Stream(broadcaster, owner, repo, cancellationToken));
            })
            .WithName("DeployStatusEvents")
            .WithSummary("Stream deployment workflow status")
            .WithDescription("Server-Sent Events stream of deploy-status events. Sends the latest status of each environment on connect, then every change. State is InProgress, Successful or Failure. Requires a bearer token with access to the repository.")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Adapts broadcaster statuses to SSE items.
    /// </summary>
    private static async IAsyncEnumerable<SseItem<DeployWorkflowStatus>> Stream(DeployStatusBroadcaster broadcaster,
        string owner, string repo,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var status in broadcaster.SubscribeAsync(owner, repo, cancellationToken))
        {
            yield return new SseItem<DeployWorkflowStatus>(status, EventType);
        }
    }
}

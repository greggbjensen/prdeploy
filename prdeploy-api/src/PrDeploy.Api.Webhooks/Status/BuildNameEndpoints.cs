using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Octokit;
using PrDeploy.Api.Webhooks.Services;

namespace PrDeploy.Api.Webhooks.Status;

/// <summary>Maps the build name lookup used by the <c>get-build-info</c> GitHub Action.</summary>
public static class BuildNameEndpoints
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>Maps <c>GET /api/build-names/{owner}/{repo}</c>, authenticated by a GitHub token that can read the repository.</summary>
    /// <param name="endpoints">Route builder the endpoint is added to.</param>
    /// <returns>The endpoint convention builder.</returns>
    public static IEndpointConventionBuilder MapBuildNames(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/api/build-names/{owner}/{repo}", async (string owner, string repo, HttpRequest request,
                BuildNameService service) =>
            {
                var token = GetBearerToken(request);
                if (token == null)
                {
                    return Results.Unauthorized();
                }

                var client = new GitHubClient(new ProductHeaderValue("prdeploy"),
                    new Octokit.Internal.InMemoryCredentialStore(new Credentials(token)));
                var names = await service.ListAsync(client, owner, repo);
                return names == null ? Results.StatusCode(StatusCodes.Status403Forbidden) : Results.Ok(names);
            })
            .WithName("BuildNames")
            .WithSummary("List build workflow service names")
            .WithDescription("Returns the workflow file to service name mappings from the repository settings. Requires a bearer GitHub token that can read the repository, such as a workflow's GITHUB_TOKEN.")
            .Produces<List<BuildName>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .AllowAnonymous(); // Authenticated by the GitHub token, not the portal JWT.
    }

    /// <summary>Reads the bearer token from the Authorization header.</summary>
    /// <param name="request">Incoming request.</param>
    /// <returns>The token, or <c>null</c> when the header is missing or not a bearer token.</returns>
    public static string? GetBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header[BearerPrefix.Length..].Trim();
        return token.Length == 0 ? null : token;
    }
}

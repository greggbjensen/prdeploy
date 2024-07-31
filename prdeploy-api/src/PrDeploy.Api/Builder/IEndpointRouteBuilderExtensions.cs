using System.Net;
using Microsoft.AspNetCore.Http.Extensions;

namespace PrDeploy.Api.Builder
{
    public static class IEndpointRouteBuilderExtensions
    {
        /// <remarks>
        /// For this to work, Banana Cake Pop UI must be disabled.
        /// <code>
        /// endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions {
        ///    Tool = { Enable = false }
        /// });
        /// </code>
        /// </remarks>
        public static IEndpointConventionBuilder MapApolloSandboxRedirect(this IEndpointRouteBuilder endpoints, 
            string pattern = "graphql")
        {
            return endpoints.MapGet(pattern, http =>
            {
                // Force URLs not on localhost to HTTPS.  This needs to be there for Kubernetes ingress.
                var displayUrl = http.Request.GetDisplayUrl();
                if (displayUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) 
                    && !displayUrl.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase))
                {
                    displayUrl = displayUrl.Replace("http://", "https://");
                }

                var endpoint = WebUtility.UrlEncode(displayUrl);
                http.Response.Redirect($"https://studio.apollographql.com/sandbox/explorer?endpoint={endpoint}");
                return Task.CompletedTask;
            });
        }
    }
}

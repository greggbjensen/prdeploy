using PrDeploy.Api.Middleware;

namespace PrDeploy.Api.Builder
{
    /// <summary>
    /// Defines the contract for application Builder Extensions operations.
    /// </summary>
    public static class IApplicationBuilderExtensions
    {
        /// <summary>
        /// Performs the use Graph Ql Standards operation and returns its result.
        /// </summary>
        /// <param name="app">Value supplied for app.</param>
        /// <returns>The value produced by this operation.</returns>
        public static IApplicationBuilder UseGraphQlStandards(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GraphQlStandardsMiddleware>(); ;
        }
    }
}

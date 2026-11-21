using PrDeploy.Api.Middleware;

namespace PrDeploy.Api.Builder
{
    /// <summary>
    /// Defines the contract for application Builder Extensions operations.
    /// </summary>
    public static class IApplicationBuilderExtensions
    {
        /// <summary>
        /// Performs the use graph ql standards operation and returns its result.
        /// </summary>
        /// <param name="app">Object containing the app data used by the operation.</param>
        /// <returns>The i application builder produced by the operation.</returns>
        public static IApplicationBuilder UseGraphQlStandards(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GraphQlStandardsMiddleware>();
        }
    }
}

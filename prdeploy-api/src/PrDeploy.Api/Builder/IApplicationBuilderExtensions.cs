using PrDeploy.Api.Middleware;

namespace PrDeploy.Api.Builder
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQlStandards(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GraphQlStandardsMiddleware>(); ;
        }

        public static IApplicationBuilder UseGlobalErrorHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalErrorHandlingMiddleware>(); ;
        }
    }
}

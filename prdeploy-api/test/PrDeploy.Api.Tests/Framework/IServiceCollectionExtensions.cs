using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace PrDeploy.Api.Tests.Framework
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddTestHttpClientFactory<TEntryPoint>(this IServiceCollection services,
            WebApplicationFactory<TEntryPoint> factory, Action<WebApplicationFactoryClientOptions> configureOptions)
            where TEntryPoint : class
        {
            var options = new WebApplicationFactoryClientOptions();
            configureOptions(options);
            services.AddTransient<IHttpClientFactory>(s => new GraphQlTestClientFactory<TEntryPoint>(factory, options));
            return services;
        }
    }
}

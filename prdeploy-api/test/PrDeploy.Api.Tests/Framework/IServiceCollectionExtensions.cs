using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace PrDeploy.Api.Tests.Framework
{
    /// <summary>Defines the contract for service Collection Extensions operations.</summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>Adds the test HTTP client factory to the associated collection.</summary>
        /// <param name="services">Services to include in the operation.</param>
        /// <param name="factory">Object containing the factory data used by the operation.</param>
        /// <param name="configureOptions">Object containing the configure options data used by the operation.</param>
        /// <typeparam name="TEntryPoint">Type of value consumed or produced by this operation.</typeparam>
        /// <returns>The i service collection produced by the operation.</returns>
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

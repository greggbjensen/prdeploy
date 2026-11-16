using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace PrDeploy.Api.Tests.Framework
{
    /// <summary>
    /// Defines the contract for service Collection Extensions operations.
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Test Http Client Factory to the associated collection.
        /// </summary>
        /// <param name="services">Services to include in the operation.</param>
        /// <param name="factory">Value supplied for factory.</param>
        /// <param name="configureOptions">Value supplied for configure Options.</param>
        /// <typeparam name="TEntryPoint">Type used by this operation.</typeparam>
        /// <returns>The value produced by this operation.</returns>
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

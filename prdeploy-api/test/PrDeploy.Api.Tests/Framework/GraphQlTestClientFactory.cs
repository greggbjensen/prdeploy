using Microsoft.AspNetCore.Mvc.Testing;

namespace PrDeploy.Api.Tests.Framework
{
    /// <summary>
    /// Represents the graph Ql Test Client Factory used by the application.
    /// </summary>
    public class GraphQlTestClientFactory<TEntryPoint> : IHttpClientFactory
        where TEntryPoint : class
    {
        private readonly WebApplicationFactory<TEntryPoint> _factory;
        private readonly WebApplicationFactoryClientOptions _options;

        /// <summary>
        /// Initializes a graph Ql Test Client Factory with the supplied dependencies.
        /// </summary>
        /// <param name="factory">Value supplied for factory.</param>
        /// <param name="options">Value supplied for options.</param>
        public GraphQlTestClientFactory(WebApplicationFactory<TEntryPoint> factory, WebApplicationFactoryClientOptions options)
        {
            _factory = factory;
            _options = options;
        }

        /// <summary>
        /// Creates the Client from the supplied inputs.
        /// </summary>
        /// <param name="name">Name used to identify the value or resource.</param>
        /// <returns>The value produced by this operation.</returns>
        public HttpClient CreateClient(string name)
        {
            var client = _factory.CreateClient(_options);
            return client;
        }
    }
}

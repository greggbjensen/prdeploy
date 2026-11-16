using Microsoft.AspNetCore.Mvc.Testing;

namespace PrDeploy.Api.Tests.Framework
{
    /// <summary>
    /// Represents graph ql test client factory data used by the application.
    /// </summary>
    public class GraphQlTestClientFactory<TEntryPoint> : IHttpClientFactory
        where TEntryPoint : class
    {
        private readonly WebApplicationFactory<TEntryPoint> _factory;
        private readonly WebApplicationFactoryClientOptions _options;

        /// <summary>
        /// Initializes the instance with its required dependencies.
        /// </summary>
        /// <param name="factory">Object containing the factory data used by the operation.</param>
        /// <param name="options">Object containing the options data used by the operation.</param>
        public GraphQlTestClientFactory(WebApplicationFactory<TEntryPoint> factory, WebApplicationFactoryClientOptions options)
        {
            _factory = factory;
            _options = options;
        }

        /// <summary>
        /// Creates the client from the supplied inputs.
        /// </summary>
        /// <param name="name">Name used to identify the value or resource.</param>
        /// <returns>The object created from the supplied inputs.</returns>
        public HttpClient CreateClient(string name)
        {
            var client = _factory.CreateClient(_options);
            return client;
        }
    }
}

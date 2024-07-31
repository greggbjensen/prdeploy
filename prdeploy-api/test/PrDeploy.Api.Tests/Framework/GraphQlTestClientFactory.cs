using Microsoft.AspNetCore.Mvc.Testing;

namespace PrDeploy.Api.Tests.Framework
{
    public class GraphQlTestClientFactory<TEntryPoint> : IHttpClientFactory
        where TEntryPoint : class
    {
        private readonly WebApplicationFactory<TEntryPoint> _factory;
        private readonly WebApplicationFactoryClientOptions _options;

        public GraphQlTestClientFactory(WebApplicationFactory<TEntryPoint> factory, WebApplicationFactoryClientOptions options)
        {
            _factory = factory;
            _options = options;
        }

        public HttpClient CreateClient(string name)
        {
            var client = _factory.CreateClient(_options);
            return client;
        }
    }
}

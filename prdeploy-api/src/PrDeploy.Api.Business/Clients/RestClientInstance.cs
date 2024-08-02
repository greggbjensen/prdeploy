using PrDeploy.Api.Business.Clients.Interfaces;
using RestSharp;

namespace PrDeploy.Api.Business.Clients
{
    public class RestClientInstance<TOptions> : IRestClientInstance<TOptions>
        where TOptions : class
    {
        public TOptions Options { get; set; } = null!;
        public IRestClient RestClient { get; set; } = null!;
    }
}

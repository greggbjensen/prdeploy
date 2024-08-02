using RestSharp;

namespace PrDeploy.Api.Business.Clients.Interfaces
{
    public interface IRestClientInstance<TOptions>
        where TOptions : class
    {
        public TOptions Options { get; set; }
        public IRestClient RestClient { get; set; }
    }
}

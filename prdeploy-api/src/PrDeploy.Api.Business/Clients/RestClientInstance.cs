using PrDeploy.Api.Business.Clients.Interfaces;
using RestSharp;

namespace PrDeploy.Api.Business.Clients
{
    /// <summary>
    /// Represents rest client instance data used by the application.
    /// </summary>
    public class RestClientInstance<TOptions> : IRestClientInstance<TOptions>
        where TOptions : class
    {
        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>Current options value.</value>
        public TOptions Options { get; set; } = null!;
        /// <summary>
        /// Gets or sets the rest client.
        /// </summary>
        /// <value>Current rest client value.</value>
        public IRestClient RestClient { get; set; } = null!;
    }
}

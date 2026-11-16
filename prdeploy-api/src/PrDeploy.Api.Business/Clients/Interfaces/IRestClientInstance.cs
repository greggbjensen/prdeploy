using RestSharp;

namespace PrDeploy.Api.Business.Clients.Interfaces
{
    /// <summary>
    /// Defines the contract for rest Client Instance operations.
    /// </summary>
    public interface IRestClientInstance<TOptions>
        where TOptions : class
    {
        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>Current options value.</value>
        public TOptions Options { get; set; }
        /// <summary>
        /// Gets or sets the rest client.
        /// </summary>
        /// <value>Current rest client value.</value>
        public IRestClient RestClient { get; set; }
    }
}

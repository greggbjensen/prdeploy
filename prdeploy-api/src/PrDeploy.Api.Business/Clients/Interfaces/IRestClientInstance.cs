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
        /// Gets or sets the Options.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public TOptions Options { get; set; }
        /// <summary>
        /// Gets or sets the Rest Client.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public IRestClient RestClient { get; set; }
    }
}

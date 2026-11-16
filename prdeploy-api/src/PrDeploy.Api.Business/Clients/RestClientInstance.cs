using PrDeploy.Api.Business.Clients.Interfaces;
using RestSharp;

namespace PrDeploy.Api.Business.Clients
{
    /// <summary>
    /// Represents the rest Client Instance used by the application.
    /// </summary>
    public class RestClientInstance<TOptions> : IRestClientInstance<TOptions>
        where TOptions : class
    {
        /// <summary>
        /// Gets or sets the Options.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public TOptions Options { get; set; } = null!;
        /// <summary>
        /// Gets or sets the Rest Client.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public IRestClient RestClient { get; set; } = null!;
    }
}

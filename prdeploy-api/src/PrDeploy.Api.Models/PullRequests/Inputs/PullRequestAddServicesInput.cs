using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.PullRequests.Inputs
{
    /// <summary>
    /// Carries input values for pull request add services operations.
    /// </summary>
    public class PullRequestAddServicesInput : RepoQueryInput
    {
        /// <summary>
        /// Gets or sets the pull number.
        /// </summary>
        /// <value>Current pull number value.</value>
        public int PullNumber { get; set; }
        /// <summary>
        /// Gets or sets the services.
        /// </summary>
        /// <value>Current services value.</value>
        public List<string> Services { get; set; } = null!;
    }
}

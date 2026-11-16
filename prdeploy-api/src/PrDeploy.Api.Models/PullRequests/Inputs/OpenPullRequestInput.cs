using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.PullRequests.Inputs
{
    /// <summary>
    /// Carries input values for the open Pull Request Input.
    /// </summary>
    public class OpenPullRequestInput : RepoQueryInput
    {
        /// <summary>
        /// Gets or sets the Search.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public string? Search { get; set; }
    }
}

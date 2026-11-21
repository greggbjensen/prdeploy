using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.PullRequests.Inputs
{
    /// <summary>Carries input values for open pull request operations.</summary>
    public class OpenPullRequestInput : RepoQueryInput
    {
        /// <summary>Gets or sets the search.</summary>
        /// <value>Current search value.</value>
        public string? Search { get; set; }
    }
}

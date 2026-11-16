using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.PullRequests.Inputs
{
    /// <summary>
    /// Carries input values for the pull Request Add Services Input.
    /// </summary>
    public class PullRequestAddServicesInput : RepoQueryInput
    {
        /// <summary>
        /// Gets or sets the Pull Number.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public int PullNumber { get; set; }
        /// <summary>
        /// Gets or sets the Services.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public List<string> Services { get; set; } = null!;
    }
}

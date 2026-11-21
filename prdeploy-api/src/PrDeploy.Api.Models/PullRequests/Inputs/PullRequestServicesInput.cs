using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.PullRequests.Inputs
{
    /// <summary>Carries input values for listing the services a pull request triggers.</summary>
    public class PullRequestServicesInput : RepoQueryInput
    {
        /// <summary>Gets or sets the pull request number.</summary>
        /// <value>Number of the pull request whose services are listed.</value>
        public int PullNumber { get; set; }
    }
}

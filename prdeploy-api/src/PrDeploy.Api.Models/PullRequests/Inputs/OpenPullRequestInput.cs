using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.PullRequests.Inputs
{
    public class OpenPullRequestInput : RepoQueryInput
    {
        public string? Search { get; set; }
    }
}

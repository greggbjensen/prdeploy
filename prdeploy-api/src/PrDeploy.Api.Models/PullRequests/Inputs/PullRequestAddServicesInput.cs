using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.PullRequests.Inputs
{
    public class PullRequestAddServicesInput : RepoQueryInput
    {
        public int PullRequestNumber { get; set; }
        public List<string> Services { get; set; } = null!;
    }
}

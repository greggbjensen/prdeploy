using HotChocolate.Types.Relay;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.DeployQueues.Inputs
{
    public class DeployQueueUpdateInput : RepoQueryInput
    {
        [ID]
        public string Environment { get; set; } = string.Empty;
        public List<int>? PullRequestNumbers { get; set; } = null!;
    }
}

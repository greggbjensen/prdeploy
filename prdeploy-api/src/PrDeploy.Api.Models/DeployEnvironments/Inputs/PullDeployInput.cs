using HotChocolate.Types.Relay;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    public class PullDeployInput : RepoQueryInput
    {
        [ID]
        public string Environment { get; set; } = string.Empty;
        public int PullNumber { get; set; }
    }
}

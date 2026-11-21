using HotChocolate.Types.Relay;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.DeployQueues.Inputs
{
    /// <summary>Carries input values for deploy queue update operations.</summary>
    public class DeployQueueUpdateInput : RepoQueryInput
    {
        /// <summary>Gets or sets the environment.</summary>
        /// <value>Current environment value.</value>
        [ID]
        public string Environment { get; set; } = string.Empty;
        /// <summary>Gets or sets the pull numbers.</summary>
        /// <value>Current pull numbers value.</value>
        public List<int>? PullNumbers { get; set; } = null!;
    }
}

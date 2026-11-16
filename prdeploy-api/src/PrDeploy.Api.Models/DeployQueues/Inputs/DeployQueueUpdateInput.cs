using HotChocolate.Types.Relay;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.DeployQueues.Inputs
{
    /// <summary>
    /// Carries input values for the deploy Queue Update Input.
    /// </summary>
    public class DeployQueueUpdateInput : RepoQueryInput
    {
        /// <summary>
        /// Gets or sets the Environment.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [ID]
        public string Environment { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Pull Numbers.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public List<int>? PullNumbers { get; set; } = null!;
    }
}

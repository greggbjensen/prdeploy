using HotChocolate.Types.Relay;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    /// <summary>
    /// Carries input values for the pull Deploy Input.
    /// </summary>
    public class PullDeployInput : RepoQueryInput
    {
        /// <summary>
        /// Gets or sets the Environment.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [ID]
        public string Environment { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the Pull Number.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public int PullNumber { get; set; }
    }
}

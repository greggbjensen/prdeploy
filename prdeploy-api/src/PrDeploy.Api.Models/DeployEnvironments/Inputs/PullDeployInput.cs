using HotChocolate.Types.Relay;
using PrDeploy.Api.Models.General.Inputs;

namespace PrDeploy.Api.Models.DeployEnvironments.Inputs
{
    /// <summary>
    /// Carries input values for pull deploy operations.
    /// </summary>
    public class PullDeployInput : RepoQueryInput
    {
        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        /// <value>Current environment value.</value>
        [ID]
        public string Environment { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the pull number.
        /// </summary>
        /// <value>Current pull number value.</value>
        public int PullNumber { get; set; }
    }
}

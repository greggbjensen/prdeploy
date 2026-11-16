using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.Settings.Inputs
{
    /// <summary>
    /// Carries input values for the set Repo Settings Input.
    /// </summary>
    [GraphQLDescription("Input for updating repo settings.")]
    public class SetRepoSettingsInput
    {
        /// <summary>
        /// Gets or sets the Owner.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Repo.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository being accessed within the owner.")]
        public string Repo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Settings.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("Full settings for owner and repository.")]
        public DeploySettings? Settings { get; set; }
    }
}

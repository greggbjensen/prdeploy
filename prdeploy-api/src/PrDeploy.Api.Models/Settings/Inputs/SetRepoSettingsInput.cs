using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.Settings.Inputs
{
    /// <summary>
    /// Carries input values for set repo settings operations.
    /// </summary>
    [GraphQLDescription("Input for updating repo settings.")]
    public class SetRepoSettingsInput
    {
        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>Current owner value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the repo.
        /// </summary>
        /// <value>Current repo value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository being accessed within the owner.")]
        public string Repo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        /// <value>Current settings value.</value>
        [GraphQLDescription("Full settings for owner and repository.")]
        public DeploySettings? Settings { get; set; }
    }
}

using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.Settings.Inputs
{
    /// <summary>
    /// Carries input values for the set Owner Settings Input.
    /// </summary>
    [GraphQLDescription("Input for updating repo settings.")]
    public class SetOwnerSettingsInput
    {
        /// <summary>
        /// Gets or sets the Owner.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Settings.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        [GraphQLDescription("Full settings for owner that defaults repo settings.")]
        public DeploySettings? Settings { get; set; }
    }
}

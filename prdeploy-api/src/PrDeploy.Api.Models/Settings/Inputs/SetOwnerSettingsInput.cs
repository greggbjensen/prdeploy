using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.Settings.Inputs
{
    /// <summary>Carries input values for set owner settings operations.</summary>
    [GraphQLDescription("Input for updating repo settings.")]
    public class SetOwnerSettingsInput
    {
        /// <summary>Gets or sets the owner.</summary>
        /// <value>Current owner value.</value>
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;

        /// <summary>Gets or sets the settings.</summary>
        /// <value>Current settings value.</value>
        [GraphQLDescription("Full settings for owner that defaults repo settings.")]
        public DeploySettings? Settings { get; set; }
    }
}

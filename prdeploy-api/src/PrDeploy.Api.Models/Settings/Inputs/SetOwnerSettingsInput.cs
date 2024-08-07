using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.Settings.Inputs
{
    [GraphQLDescription("Input for updating repo settings.")]
    public class SetOwnerSettingsInput
    {
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;

        [GraphQLDescription("Full settings for owner that defaults repo settings.")]
        public DeploySettings? Settings { get; set; }
    }
}

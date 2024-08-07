using HotChocolate;
using HotChocolate.Types;

namespace PrDeploy.Api.Models.Settings.Inputs
{
    [GraphQLDescription("Input for updating repo settings.")]
    public class SetRepoSettingsInput
    {
        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository owner or organization.")]
        public string Owner { get; set; } = string.Empty;

        [GraphQLType(typeof(IdType))]
        [GraphQLDescription("Repository being accessed within the owner.")]
        public string Repo { get; set; } = string.Empty;

        [GraphQLDescription("Full settings for owner and repository.")]
        public DeploySettings? Settings { get; set; }
    }
}

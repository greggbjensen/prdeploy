using HotChocolate;
using PrDeploy.Api.Models.Settings.Compare;

namespace PrDeploy.Api.Models.Settings
{
    [GraphQLDescription("All available settings for an owner and repo.")]
    public class AllSettings
    {
        public DeploySettingsCompare DeploySettingsCompare { get; set; } = null!;
    }
}

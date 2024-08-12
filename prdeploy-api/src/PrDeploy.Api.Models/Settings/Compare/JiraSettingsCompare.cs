namespace PrDeploy.Api.Models.Settings.Compare
{
    public class JiraSettingsCompare
    {
        public OwnerRepoValue<bool?> AddIssuesEnabled { get; set; } = null!;
        public OwnerRepoValue<string> Host { get; set; } = null!;
        public OwnerRepoValue<string> Username { get; set; } = null!;
        public OwnerRepoValue<string> Password { get; set; } = null!;
    }
}

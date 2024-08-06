namespace PrDeploy.Api.Models.Settings.Compare
{
    public class JiraSettingsCompare
    {
        public OwnerRepoValue<bool?> AddIssuesEnabled { get; set; }
        public OwnerRepoValue<string> Host { get; set; }
        public OwnerRepoValue<string> Username { get; set; }
        public OwnerRepoValue<string> Password { get; set; }
    }
}

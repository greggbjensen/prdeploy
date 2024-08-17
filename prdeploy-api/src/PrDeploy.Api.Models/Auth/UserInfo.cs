namespace PrDeploy.Api.Models.Auth
{
    public class UserInfo
    {
        public long Id { get; set; }

        public string AvatarUrl { get; set; } = string.Empty;

        public string Login { get; set; }

        public string Name { get; set; }

        public bool Admin { get; set; }

        public GitHubAccountType? Type { get; set; }
    }
}

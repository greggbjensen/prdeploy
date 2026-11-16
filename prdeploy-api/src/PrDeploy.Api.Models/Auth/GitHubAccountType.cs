namespace PrDeploy.Api.Models.Auth
{
    /// <summary>
    /// Represents the git Hub Account Type used by the application.
    /// </summary>
    public enum GitHubAccountType
    {
        User = 0,
        Organization = 1,
        Bot = 2,
        Mannequin = 3
    }
}

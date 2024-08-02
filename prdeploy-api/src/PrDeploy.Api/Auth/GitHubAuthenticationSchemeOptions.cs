using Microsoft.AspNetCore.Authentication;

namespace PrDeploy.Api.Auth
{
    public class GitHubAuthenticationSchemeOptions: AuthenticationSchemeOptions
    {
        public const string SchemeName = "GitHubAuth";
    }
}

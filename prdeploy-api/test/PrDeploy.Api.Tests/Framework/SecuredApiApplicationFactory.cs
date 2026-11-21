using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Octokit;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Tests.Webhooks.Framework;

namespace PrDeploy.Api.Tests.Framework;

/// <summary>
/// Starts the API with its real access checks and a mocked GitHub API, so tests exercise authorization end to end
/// over HTTP. Only the external GitHub service is replaced.
/// </summary>
public class SecuredApiApplicationFactory : DeployApiApplicationFactory
{
    /// <summary>
    /// Login the test authentication handler signs in as.
    /// </summary>
    public const string Login = "test-user";

    /// <summary>
    /// Gets the GitHub API mock shared by every request to this host. Tests run one at a time within a class, so a test
    /// may reset and configure it in its constructor.
    /// </summary>
    public Mock<IGitHubClient> GitHub { get; } = new() { DefaultValue = DefaultValue.Mock };

    /// <inheritdoc />
    protected override bool UseRealGitHubSecurity => true;

    /// <summary>
    /// Allows the test owner organization and replaces the GitHub client with <see cref="GitHub"/>.
    /// </summary>
    /// <param name="builder">Host builder being configured.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<GitHubAuthOptions>(o => o.AllowedOrganizations = [WebhookTestHost.Owner]);
            services.RemoveAll<IGitHubClient>();
            services.AddSingleton(GitHub.Object);
        });
    }
}

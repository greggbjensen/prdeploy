using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Tests.Constants;
using PrDeploy.Api.Tests.Client;

namespace PrDeploy.Api.Tests.Framework;

/// <summary>Contains tests that verify deploy API behavior.</summary>
public class DeployApiTest : IClassFixture<DeployApiApplicationFactory>
{
    public static readonly DeployEnvironmentData DeployEnvironments = new();
    public static readonly GitHubData GitHub = new();
    public static readonly PullRequestData PullRequests = new();
    public static readonly DeployUserData DeployUsers = new();

    private readonly Lazy<IDeployApiClient> _client;

    //// SourceRef: https://graphql-aspnet.github.io/docs/development/unit-testing
    /// <summary>Gets or sets the factory.</summary>
    /// <value>Current factory value.</value>
    protected DeployApiApplicationFactory Factory { get; set; }

    /// <summary>Gets the client.</summary>
    /// <value>Current client value.</value>
    protected IDeployApiClient Client => _client.Value;

    /// <summary>Initializes the test fixture and prepares its dependencies.</summary>
    /// <param name="factory">Object containing the factory data used by the operation.</param>
    public DeployApiTest(DeployApiApplicationFactory factory)
    {
        Factory = factory;

        // This should be lazy loaded to prevent early service instantiation.
        _client = new Lazy<IDeployApiClient>(InitializeTest);
        Factory.ConfigureServices = ConfigureServices;
        Factory.ConfigureTestServices = ConfigureTestServices;
    }

    /// <summary>Configures the services for the current application or test.</summary>
    /// <param name="services">Services to include in the operation.</param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>Configures dependency injection services for the test application.</summary>
    /// <param name="services">Services to include in the operation.</param>
    public virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    /// <summary>Performs the initialize test operation and returns its result.</summary>
    /// <returns>The i deploy API client produced by the operation.</returns>
    private IDeployApiClient InitializeTest()
    {
        var serviceProvider = Factory.Services;
        return serviceProvider.GetRequiredService<IDeployApiClient>();
    }
}

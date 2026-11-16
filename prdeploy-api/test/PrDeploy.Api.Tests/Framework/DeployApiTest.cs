using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Tests.Constants;
using PrDeploy.Api.Tests.Client;

namespace PrDeploy.Api.Tests.Framework;

/// <summary>
/// Contains tests that verify the behavior of the deploy Api Test.
/// </summary>
public class DeployApiTest : IClassFixture<DeployApiApplicationFactory>
{
    public static readonly DeployEnvironmentData DeployEnvironments = new();
    public static readonly GitHubData GitHub = new();
    public static readonly PullRequestData PullRequests = new();
    public static readonly DeployUserData DeployUsers = new();

    private readonly Lazy<IDeployApiClient> _client;

    //// SourceRef: https://graphql-aspnet.github.io/docs/development/unit-testing
    /// <summary>
    /// Gets or sets the Factory.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    protected DeployApiApplicationFactory Factory { get; set; }

    /// <summary>
    /// Gets the Client.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    protected IDeployApiClient Client => _client.Value;

    /// <summary>
    /// Initializes the test fixture and prepares its dependencies.
    /// </summary>
    /// <param name="factory">Value supplied for factory.</param>
    public DeployApiTest(DeployApiApplicationFactory factory)
    {
        Factory = factory;

        // This should be lazy loaded to prevent early service instantiation.
        _client = new Lazy<IDeployApiClient>(InitializeTest);
        Factory.ConfigureServices = ConfigureServices;
        Factory.ConfigureTestServices = ConfigureTestServices;
    }

    /// <summary>
    /// Performs the configure Services operation.
    /// </summary>
    /// <param name="services">Services to include in the operation.</param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Performs the configure Test Services operation.
    /// </summary>
    /// <param name="services">Services to include in the operation.</param>
    public virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Performs the initialize Test operation and returns its result.
    /// </summary>
    /// <returns>The value produced by this operation.</returns>
    private IDeployApiClient InitializeTest()
    {
        var serviceProvider = Factory.Services;
        return serviceProvider.GetRequiredService<IDeployApiClient>();
    }
}

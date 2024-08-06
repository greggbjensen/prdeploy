using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Tests.Constants;
using PrDeploy.Api.Tests.Client;
using System.Net.Sockets;

namespace PrDeploy.Api.Tests.Framework;

public class DeployApiTest : IClassFixture<DeployApiApplicationFactory>
{
    public static readonly DeployEnvironmentData DeployEnvironments = new();
    public static readonly GitHubData GitHub = new();
    public static readonly PrDeployOptionsData PrDeployOptions = new();
    public static readonly ParameterVariables ParameterVariables = new();
    public static readonly PullRequestData PullRequests = new();
    public static readonly DeployUserData DeployUsers = new();

    private readonly Lazy<IDeployApiClient> _client;

    //// SourceRef: https://graphql-aspnet.github.io/docs/development/unit-testing
    protected DeployApiApplicationFactory Factory { get; set; }

    protected IDeployApiClient Client => _client.Value;

    public DeployApiTest(DeployApiApplicationFactory factory)
    {
        Factory = factory;

        // This should be lazy loaded to prevent early service instantiation.
        _client = new Lazy<IDeployApiClient>(InitializeTest);
        Factory.ConfigureServices = ConfigureServices;
        Factory.ConfigureTestServices = ConfigureTestServices;
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    private IDeployApiClient InitializeTest()
    {
        var serviceProvider = Factory.Services;
        return serviceProvider.GetRequiredService<IDeployApiClient>();
    }
}

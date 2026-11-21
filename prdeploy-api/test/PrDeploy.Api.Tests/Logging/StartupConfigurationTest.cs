using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PrDeploy.Api.Tests.Framework;

namespace PrDeploy.Api.Tests.Logging;

/// <summary>Verifies which configuration sources the API reads when it starts.</summary>
/// <param name="factory">Application factory used to start the API under test.</param>
[Collection(ApiHostCollection.Name)]
public class StartupConfigurationTest(DeployApiApplicationFactory factory) : DeployApiTest(factory)
{
    private const string MarkerVariable = "PrDeployDotEnvTest__Marker";
    private const string MarkerKey = "PrDeployDotEnvTest:Marker";

    /// <summary>Removes hosted services so starting the host only builds configuration and services.</summary>
    /// <param name="services">Services registered for the test host.</param>
    public override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IHostedService>();
    }

    /// <summary>A <c>.env</c> file in the working directory must not be loaded into environment variables or configuration, because it would silently override the YAML appsettings files.</summary>
    [Fact]
    public void Startup_IgnoresDotEnvFileInWorkingDirectory()
    {
        var dotEnvPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        Assert.False(File.Exists(dotEnvPath), $"Refusing to overwrite an existing file at {dotEnvPath}.");

        File.WriteAllText(dotEnvPath, $"{MarkerVariable}=loaded\n");
        try
        {
            var configuration = Factory.Services.GetRequiredService<IConfiguration>();

            Assert.Null(Environment.GetEnvironmentVariable(MarkerVariable));
            Assert.Null(configuration[MarkerKey]);
        }
        finally
        {
            File.Delete(dotEnvPath);
            Environment.SetEnvironmentVariable(MarkerVariable, null);
        }
    }
}

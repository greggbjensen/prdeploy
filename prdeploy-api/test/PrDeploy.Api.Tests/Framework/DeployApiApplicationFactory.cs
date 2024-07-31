using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace PrDeploy.Api.Tests.Framework;

public class DeployApiApplicationFactory : WebApplicationFactory<Program>
{
    public Action<IServiceCollection>? ConfigureServices { get; set; }

    public Action<IServiceCollection> ConfigureTestServices { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    { ;
        builder.ConfigureTestServices(ConfigureTestServices);

        builder.ConfigureServices(services =>
        {
            // Services used in all tests.
            // Adds a factory for GraphQL client to use Test Server HttpClient.
            services.AddTestHttpClientFactory(this, options =>
            {
                options.BaseAddress = new Uri("http://localhost/graphql/");
            });

            services.AddDeployApiClient();

            // Call configure services on test.
            ConfigureServices(services);
        });
    }
}

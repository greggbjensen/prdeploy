using Amazon;
using Amazon.Runtime;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Services;
using PrDeploy.Api.Business.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Amazon.SimpleSystemsManagement;
using PrDeploy.Api.Business.Clients.Interfaces;
using PrDeploy.Api.Business.Clients;
using RestSharp;
using PrDeploy.Api.Business.Security;
using PrDeploy.Api.Business.Security.Interfaces;
using PrDeploy.Api.Business.Stores;
using PrDeploy.Api.Business.Stores.Interfaces;
using Amazon.SecurityToken;
using PrDeploy.Api.Business.Auth;
using PrDeploy.Api.Business.Auth.Interfaces;

namespace PrDeploy.Api.Business;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPrDeployApiBusiness(this IServiceCollection services, IConfiguration configuration)
    {

        services

            // GitHub.
            .Configure<GitHubAuthOptions>(configuration.GetSection("GitHubAuth"))
            .Configure<JwtOptions>(configuration.GetSection("Jwt"))
            .Configure<AwsExtendedOptions>(configuration.GetSection("Aws"))
            .AddScoped<IRestClientInstance<GitHubAuthOptions>>(s =>
            {
                var options = s.GetRequiredService<IOptions<GitHubAuthOptions>>();
                return new RestClientInstance<GitHubAuthOptions>
                {
                    Options = options.Value,
                    RestClient = new RestClient(new RestClientOptions(options.Value.Authority))
                };
            })

            // AWS.
            .AddDefaultAWSOptions(configuration.GetAWSOptions())
            .AddAWSService<IAmazonSecurityTokenService>()
            .AddAWSService<IAmazonSimpleSystemsManagement>()

            // Cache.
            .AddMemoryCache()

            // Stores.
            .AddScoped<IParameterStore, ParameterStore>()

            // Services.
            .AddScoped<IDeployQueueService, DeployQueueService>()
            .AddScoped<IDeployEnvironmentService, DeployEnvironmentService>()
            .AddScoped<IPullRequestService, PullRequestService>()
            .AddScoped<IDeploySettingsService, DeploySettingsService>()
            .AddScoped<IOwnerRepoService, OwnerRepoService>()
            .AddScoped<IGitHubAuthClient, GitHubAuthClient>()
            .AddScoped<IGitHubSecurity, GitHubSecurity>()
            .AddScoped<ICipherService, CipherService>();

        return services;
    }
}

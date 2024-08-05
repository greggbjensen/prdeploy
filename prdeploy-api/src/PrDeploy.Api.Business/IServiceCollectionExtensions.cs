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

namespace PrDeploy.Api.Business;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPrDeployApiBusiness(this IServiceCollection services, IConfiguration configuration)
    {

        services
            .Configure<AwsOptions>(configuration.GetSection("Aws"))
            .Configure<PrDeployOptions>(configuration.GetSection("PrDeploy"))
            .Configure<GitHubAuthOptions>(configuration.GetSection("GitHubAuth"))
            .AddScoped<IRestClientInstance<GitHubAuthOptions>>(s =>
            {
                var options = s.GetRequiredService<IOptions<GitHubAuthOptions>>();
                return new RestClientInstance<GitHubAuthOptions>
                {
                    Options = options.Value,
                    RestClient = new RestClient(new RestClientOptions(options.Value.Authority))
                };
            })
            .AddScoped<IAmazonSimpleSystemsManagement>(s =>
            {
                var options = s.GetRequiredService<IOptions<AwsOptions>>().Value;
                var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
                return new AmazonSimpleSystemsManagementClient(credentials, RegionEndpoint.GetBySystemName(options.Region));
            })

            .AddMemoryCache()

            // Stores.
            .AddScoped<IParameterStore, ParameterStore>()

            // Services.
            .AddScoped<IDeployQueueService, DeployQueueService>()
            .AddScoped<IDeployEnvironmentService, DeployEnvironmentService>()
            .AddScoped<IPullRequestService, PullRequestService>()
            .AddScoped<IRepoSettingsService, RepoSettingsService>()
            .AddScoped<IRepositoryService, RepositoryService>()
            .AddScoped<IGitHubAuthClient, GitHubAuthClient>()
            .AddScoped<IRepositorySecurity, RepositorySecurity>();

        return services;
    }
}

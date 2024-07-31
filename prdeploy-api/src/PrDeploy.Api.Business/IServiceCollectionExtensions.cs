using System.Reflection;
using Amazon;
using Amazon.Runtime;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Business.Services;
using PrDeploy.Api.Business.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Octokit.Internal;
using Octokit;
using Amazon.SimpleSystemsManagement;

namespace PrDeploy.Api.Business;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPrDeployApiBusiness(this IServiceCollection services, IConfiguration configuration)
    {

        services
            .Configure<AwsOptions>(options =>
            {
                configuration.Bind("Aws", options);
                BindFromEnvironment(options);
            })
            .Configure<GitHubOptions>(options =>
            {
                configuration.Bind("GitHub", options);
                BindFromEnvironment(options);
            })
            .AddScoped<IGitHubClient>(s =>
            {
                var options = s.GetRequiredService<IOptions<GitHubOptions>>().Value;
                var credentials = new Credentials(options.Token);
                return new GitHubClient(
                    new ProductHeaderValue("prdeploy"), new InMemoryCredentialStore(credentials));
            })
            .AddScoped<IAmazonSimpleSystemsManagement>(s =>
            {
                var options = s.GetRequiredService<IOptions<AwsOptions>>().Value;
                var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
                return new AmazonSimpleSystemsManagementClient(credentials, RegionEndpoint.GetBySystemName(options.Region));
            })

            .AddMemoryCache()

            // Services.
            .AddScoped<IDeployQueueService, DeployQueueService>()
            .AddScoped<IDeployEnvironmentService, DeployEnvironmentService>()
            .AddScoped<IPullRequestService, PullRequestService>()
            .AddScoped<IRepoSettingsService, RepoSettingsService>()
            .AddScoped<IRepositoryService, RepositoryService>();

        return services;
    }

    private static void BindFromEnvironment<T>(T options)
    {
        // Support binding to environment variable for local development.
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.PropertyType == typeof(string));
        foreach (var property in properties)
        {
            var value = (string?)property.GetValue(options);
            if (value?.StartsWith("<ENV:") == true)
            {
                var tokenName = value.Trim('>').Split(":")[1];
                var variableValue = Environment.GetEnvironmentVariable(tokenName);
                property.SetValue(options, variableValue);
            }
        }
    }
}

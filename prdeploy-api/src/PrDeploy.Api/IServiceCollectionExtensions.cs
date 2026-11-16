using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;
using Octokit.Internal;
using Octokit;
using PrDeploy.Api.Business;
using PrDeploy.Api.Business.Stores;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Filters;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Schema.Mutations;
using PrDeploy.Api.Schema.Queries;
using PrDeploy.Api.Business.Services;

namespace PrDeploy.Api;

/// <summary>
/// Registers the API's GraphQL, authentication, and persistence services.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds the pr deploy API to the associated collection.
    /// </summary>
    /// <param name="services">Services to include in the operation.</param>
    /// <param name="configuration">Object containing the configuration data used by the operation.</param>
    /// <returns>The i service collection produced by the operation.</returns>
    public static IServiceCollection AddPrDeployApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPrDeployStorage(configuration);

        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType(q => q.Name("DeployQuery"))
            .AddType<DeployQueueQuery>()
            .AddType<DeploySettingsQuery>()
            .AddType<DeployEnvironmentQuery>()
            .AddType<PullRequestQuery>()
            .AddType<OwnerRepoQuery>()
            .AddType<RepositoryQuery>()
            .AddMutationType(m => m.Name("DeployMutation"))
            .AddType<DeployEnvironmentMutation>()
            .AddType<DeployQueueMutation>()
            .AddType<DeploySettingsMutation>()
            .AddType<OwnerRepoMutation>()
            .AddType<PullRequestMutation>()

            // Error handling. HotChocolate 16 resolves the filter from schema services,
            // so the application services it depends on must be exposed explicitly.
            .AddApplicationService<ILogger<SanitizedErrorFilter>>()
            .AddApplicationService<IHostEnvironment>()
            .AddErrorFilter<SanitizedErrorFilter>()
            .AddHttpResponseFormatter<SanitizedHttpResponseFormatter>();

        // Register service classes so they can be injected without a [Service] attribute.
        services
            .AddScoped<DeployQueueService>()
            .AddScoped<DeployEnvironmentService>()
            .AddScoped<PullRequestService>()
            .AddScoped<OwnerRepoService>()
            .AddScoped<DeploySettingsService>();

        services
            .AddHttpContextAccessor()
            .AddScoped<ISecurityContext, SecurityContext>()
            .AddScoped<IGitHubClient>(s =>
            {
                var userToken = s.GetRequiredService<ISecurityContext>().UserToken;
                if (string.IsNullOrEmpty(userToken))
                {
                    throw new HttpRequestException("No token in security context.", null, HttpStatusCode.Unauthorized);
                }

                var credentials = new Credentials(userToken);
                return new GitHubClient(
                    new ProductHeaderValue("prdeploy"), new InMemoryCredentialStore(credentials));
            })
            .AddScoped<IAuthorizationHandler, GitHubRepoAuthorizationHandler>();

        ConfigureDataProtectionKeyStorage(services, configuration);

        return services;
    }

    /// <summary>
    /// Selects the Data Protection key repository associated with the configured storage provider.
    /// </summary>
    private static void ConfigureDataProtectionKeyStorage(
        IServiceCollection services,
        IConfiguration configuration)
    {
        const string keyStorageSection = "DataProtection:KeyStorage";
        var provider = StorageProviderConfiguration.GetProvider(configuration);
        var dataProtection = services.AddDataProtection();

        if (provider == StorageProvider.AWS &&
            UsesLegacySystemsManagerForDataProtection(configuration))
        {
            var storage = configuration.GetSection(keyStorageSection);
            var parameterPath = configuration["Storage:AWS:DataProtectionParameterPath"] ??
                storage["ParameterPath"] ??
                $"{(configuration["Storage:DeploymentValuePathPrefix"] ??
                   configuration["Aws:SecretPathPrefix"])?.TrimEnd('/')}/DATA_PROTECTION";
            if (string.IsNullOrWhiteSpace(parameterPath) ||
                !parameterPath.StartsWith("/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "'Storage:AWS:DataProtectionParameterPath' must be an AWS Systems Manager Parameter Store path starting with '/'.");
            }

            dataProtection.PersistKeysToAWSSystemsManager(parameterPath);
        }
        else
        {
            services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(serviceProvider =>
                new ConfigureNamedOptions<KeyManagementOptions>(
                    Microsoft.Extensions.Options.Options.DefaultName,
                    options => options.XmlRepository = serviceProvider.GetRequiredService<IXmlRepository>()));
        }

        dataProtection.UseCryptographicAlgorithms(new()
        {
            EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
            ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
        });
    }

    /// <summary>
    /// Checks whether AWS Systems Manager Parameter Store was selected for Data Protection keys.
    /// </summary>
    private static bool UsesLegacySystemsManagerForDataProtection(IConfiguration configuration)
    {
        var keyStorage = configuration.GetSection("DataProtection:KeyStorage");
        return !string.IsNullOrWhiteSpace(configuration["Storage:AWS:DataProtectionParameterPath"]) ||
               string.Equals(keyStorage["Provider"], "AWSSystemsManager", StringComparison.OrdinalIgnoreCase) ||
               !string.IsNullOrWhiteSpace(keyStorage["ParameterPath"]);
    }
}

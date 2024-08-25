using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Octokit.Internal;
using Octokit;
using PrDeploy.Api.Auth;
using PrDeploy.Api.Business.Services.Interfaces;
using PrDeploy.Api.Filters;
using PrDeploy.Api.Business.Auth.Interfaces;
using PrDeploy.Api.Business.Options;
using PrDeploy.Api.Schema.Mutations;
using PrDeploy.Api.Schema.Queries;

namespace PrDeploy.Api;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPrDeployApi(this IServiceCollection services, IConfiguration configuration)
    {
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

            // Register service classes so they can be injected without a [Service] attribute.
            .RegisterService<IDeployQueueService>()
            .RegisterService<IDeployEnvironmentService>()
            .RegisterService<IPullRequestService>()
            .RegisterService<IOwnerRepoService>()
            .RegisterService<IDeploySettingsService>()

            // Error handling.
            .AddErrorFilter<SanitizedErrorFilter>();

        services
            .AddHttpResponseFormatter<SanitizedHttpResponseFormatter>()
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

        var awsOptions = new AwsExtendedOptions();
        configuration.Bind("AWS", awsOptions);
        services.AddDataProtection()
                .PersistKeysToAWSSystemsManager($"{awsOptions.SecretPathPrefix}/DATA_PROTECTION")
                .UseCryptographicAlgorithms(new()
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                });

        return services;
    }
}

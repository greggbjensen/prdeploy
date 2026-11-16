using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Models.Auth;
using PrDeploy.Api.Models.Auth.Validators;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.DeployEnvironments.Inputs.Validators;
using PrDeploy.Api.Models.General.Inputs;
using PrDeploy.Api.Models.General.Inputs.Validators;

namespace PrDeploy.Api.Models
{
    /// <summary>
    /// Defines the contract for service Collection Extensions operations.
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the pr deploy API model validation to the associated collection.
        /// </summary>
        /// <param name="services">Services to include in the operation.</param>
        /// <returns>The i service collection produced by the operation.</returns>
        public static IServiceCollection AddPrDeployApiModelValidation(this IServiceCollection services)
        {
            services
                .AddScoped<IValidator<AccessTokenRequest>, AccessTokenRequestValidator>()
                .AddScoped<IValidator<DeployStateComparisonInput>, DeployStateComparisonInputValidator>()
                .AddScoped<IValidator<RepoQueryInput>, RepositoryQueryInputValidator>();

            return services;
        }
    }
}

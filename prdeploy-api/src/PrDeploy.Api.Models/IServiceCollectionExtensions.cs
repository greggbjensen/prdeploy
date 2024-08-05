using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Models.Auth;
using PrDeploy.Api.Models.Auth.Validators;
using PrDeploy.Api.Models.DeployEnvironments.Inputs;
using PrDeploy.Api.Models.DeployEnvironments.Inputs.Validators;

namespace PrDeploy.Api.Models
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddPrDeployApiModelValidation(this IServiceCollection services)
        {
            services
                .AddScoped<IValidator<AccessTokenRequest>, AccessTokenRequestValidator>()
                .AddScoped<IValidator<DeployStateComparisonInput>, DeployStateComparisonInputValidator>()
                .AddScoped<IValidator<EnvironmentsInput>, EnvironmentsInputValidator>();

            return services;
        }
    }
}

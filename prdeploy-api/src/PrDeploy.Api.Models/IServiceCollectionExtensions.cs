using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PrDeploy.Api.Models.Validation;

namespace PrDeploy.Api.Models
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddPrDeployApiModelValidation(this IServiceCollection services)
        {
            services.AddScoped<IValidator<AccessTokenRequest>, AccessTokenRequestValidator>();

            return services;
        }
    }
}

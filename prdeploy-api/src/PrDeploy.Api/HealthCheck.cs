using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PrDeploy.Api
{
    /// <summary>Represents health check data used by the application.</summary>
    internal class HealthCheck : IHealthCheck
    {
        /// <summary>Gets the build number.</summary>
        /// <value>Current build number value.</value>
        public static string BuildNumber { get; }
        /// <summary>Gets the code version.</summary>
        /// <value>Current code version value.</value>
        public static string CodeVersion { get; }

        /// <summary>Initializes the instance with its required dependencies.</summary>
        static HealthCheck()
        {
            var attr = typeof(HealthCheck).Assembly.GetCustomAttributes(true);

            var fileVersionAttr = attr.
                OfType<System.Reflection.AssemblyFileVersionAttribute>()
                .FirstOrDefault();

            var infoVersionAttr = attr.
                OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();
            BuildNumber = fileVersionAttr != null ? fileVersionAttr.Version : "N/A";
            CodeVersion = infoVersionAttr != null ? infoVersionAttr.InformationalVersion : "N/A";

        }

        /// <summary>Checks whether the health condition is satisfied.</summary>
        /// <param name="context">Request or application context used by the operation.</param>
        /// <param name="cancellationToken">Object containing the cancellation token data used by the operation.</param>
        /// <returns>The health check result produced by the operation.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, object>
            {
                { "BuildNumber", BuildNumber },
                { "CodeVersion", CodeVersion }
            };
            
            return Task.FromResult(HealthCheckResult.Healthy(data: data));
        }
	}
}

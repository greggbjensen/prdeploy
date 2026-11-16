using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PrDeploy.Api
{
    /// <summary>
    /// Represents the health Check used by the application.
    /// </summary>
    internal class HealthCheck : IHealthCheck
    {
        /// <summary>
        /// Gets the Build Number.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public static string BuildNumber { get; }
        /// <summary>
        /// Gets the Code Version.
        /// </summary>
        /// <returns>The value exposed by this member.</returns>
        public static string CodeVersion { get; }

        /// <summary>
        /// Initializes a health Check with the supplied dependencies.
        /// </summary>
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

        /// <summary>
        /// Checks whether the health condition is satisfied.
        /// </summary>
        /// <param name="context">Request or application context used by the operation.</param>
        /// <param name="cancellationToken">Value supplied for cancellation Token.</param>
        /// <returns>The value produced by this operation.</returns>
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

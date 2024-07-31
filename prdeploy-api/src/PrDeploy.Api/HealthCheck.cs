using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PrDeploy.Api
{
    internal class HealthCheck : IHealthCheck
    {
        public static string BuildNumber { get; }
        public static string CodeVersion { get; }

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

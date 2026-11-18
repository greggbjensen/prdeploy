using Serilog;
using Serilog.Formatting.Json;

namespace PrDeploy.Api.Configuration
{
    /// <summary>
    /// Builds the Serilog pipeline shared by the bootstrap logger and the host logger so both read the same
    /// <c>Serilog</c> configuration section.
    /// </summary>
    public static class SerilogConfiguration
    {
        /// <summary>
        /// Applies the <c>Serilog</c> configuration section, log-context enrichment, and the JSON console sink.
        /// </summary>
        /// <param name="loggerConfiguration">Configuration to extend.</param>
        /// <param name="configuration">Application configuration holding the <c>Serilog</c> section.</param>
        /// <param name="services">
        /// Optional service provider; when supplied, sinks and enrichers registered in DI are also applied.
        /// </param>
        /// <returns>The same <paramref name="loggerConfiguration"/> for chaining.</returns>
        public static LoggerConfiguration Configure(
            LoggerConfiguration loggerConfiguration,
            IConfiguration configuration,
            IServiceProvider? services = null)
        {
            loggerConfiguration = loggerConfiguration.ReadFrom.Configuration(configuration);
            if (services != null)
            {
                loggerConfiguration = loggerConfiguration.ReadFrom.Services(services);
            }

            // Includes webhook scopes such as Owner, Repo and PullNumber.
            return loggerConfiguration
                .Enrich.FromLogContext()
                .WriteTo.Console(new JsonFormatter(renderMessage: true));
        }
    }
}

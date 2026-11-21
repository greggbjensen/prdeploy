using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

namespace PrDeploy.Api.Configuration
{
    /// <summary>Builds the Serilog pipeline shared by the bootstrap logger and the host logger so both read the same <c>Serilog</c> configuration section.</summary>
    public static class SerilogConfiguration
    {
        /// <summary>Output template for the development console: timestamp, level, source, message, properties, exception.</summary>
        internal const string PrettyOutputTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}";

        /// <summary>Applies the <c>Serilog</c> configuration section, log-context enrichment, and and the console sink: colored human-readable text for development, structured JSON otherwise.</summary>
        /// <param name="loggerConfiguration">Configuration to extend.</param>
        /// <param name="configuration">Application configuration holding the <c>Serilog</c> section.</param>
        /// <param name="services">
        /// Optional service provider; when supplied, sinks and enrichers registered in DI are also applied.
        /// </param>
        /// <param name="prettyConsole">
        /// When <c>true</c>, writes colorized single-line text with properties for local reading; when
        /// <c>false</c>, writes one structured JSON document per event for log aggregation.
        /// </param>
        /// <returns>The same <paramref name="loggerConfiguration"/> for chaining.</returns>
        public static LoggerConfiguration Configure(
            LoggerConfiguration loggerConfiguration,
            IConfiguration configuration,
            IServiceProvider? services = null,
            bool prettyConsole = false)
        {
            loggerConfiguration = loggerConfiguration.ReadFrom.Configuration(configuration);
            if (services != null)
            {
                loggerConfiguration = loggerConfiguration.ReadFrom.Services(services);
            }

            // Includes webhook scopes such as Owner, Repo and PullNumber.
            loggerConfiguration = loggerConfiguration.Enrich.FromLogContext();
            return prettyConsole
                ? loggerConfiguration.WriteTo.Console(
                    outputTemplate: PrettyOutputTemplate,
                    theme: AnsiConsoleTheme.Code)
                : loggerConfiguration.WriteTo.Console(new JsonFormatter(renderMessage: true));
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace PrDeploy.Api.Configuration
{
    /// <summary>Extensions for loading YAML configuration files.</summary>
    public static class YamlConfigurationExtensions
    {
        /// <summary>Adds a YAML file as a configuration source.</summary>
        /// <param name="builder">Builder to extend.</param>
        /// <param name="path">File path, relative to the builder's base path.</param>
        /// <param name="optional">Whether a missing file is ignored.</param>
        /// <param name="reloadOnChange">Whether the file is watched and reloaded.</param>
        /// <returns>The same builder for chaining.</returns>
        public static IConfigurationBuilder AddYamlFile(
            this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
        {
            return builder.Add(CreateSource(builder, path, optional, reloadOnChange));
        }

        /// <summary>Replaces the default <c>appsettings.json</c> and <c>appsettings.{environment}.json</c> sources with <c>appsettings.yaml</c> and <c>appsettings.{environment}.yaml</c> at the same position, so precedence against user secrets, environment variables, and the command line is unchanged.</summary>
        /// <param name="builder">Builder created by <c>WebApplication.CreateBuilder</c>.</param>
        /// <param name="environmentName">Current hosting environment name.</param>
        /// <returns>The same builder for chaining.</returns>
        public static IConfigurationBuilder UseYamlAppSettings(this IConfigurationBuilder builder, string environmentName)
        {
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["appsettings.json"] = "appsettings.yaml",
                [$"appsettings.{environmentName}.json"] = $"appsettings.{environmentName}.yaml"
            };

            for (var i = 0; i < builder.Sources.Count; i++)
            {
                if (builder.Sources[i] is JsonConfigurationSource json
                    && json.Path != null
                    && replacements.TryGetValue(json.Path, out var yamlPath))
                {
                    var yaml = CreateSource(builder, yamlPath, json.Optional, json.ReloadOnChange);
                    yaml.FileProvider = json.FileProvider;
                    builder.Sources[i] = yaml;
                }
            }

            return builder;
        }

        /// <summary>Creates a configured YAML source rooted at the builder's file provider.</summary>
        private static YamlConfigurationSource CreateSource(
            IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
        {
            var source = new YamlConfigurationSource
            {
                Path = path,
                Optional = optional,
                ReloadOnChange = reloadOnChange
            };
            return source;
        }
    }
}

using Microsoft.Extensions.Configuration;
using YamlDotNet.RepresentationModel;

namespace PrDeploy.Api.Configuration
{
    /// <summary>Configuration source that reads a YAML file into the standard colon-delimited configuration keys.</summary>
    public class YamlConfigurationSource : FileConfigurationSource
    {
        /// <summary>Creates the provider that parses the file.</summary>
        /// <param name="builder">Configuration builder, used to resolve the file provider.</param>
        /// <returns>A <see cref="YamlConfigurationProvider"/> for this source.</returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new YamlConfigurationProvider(this);
        }
    }

    /// <summary>Loads configuration from a YAML file. Mappings become nested sections and sequences become indexed children (<c>Section:0</c>, <c>Section:1</c>, …), matching the layout produced by the JSON provider.</summary>
    public class YamlConfigurationProvider : FileConfigurationProvider
    {
        /// <summary>Creates a provider for the given source.</summary>
        /// <param name="source">Source describing the file to read.</param>
        public YamlConfigurationProvider(YamlConfigurationSource source) : base(source)
        {
        }

        /// <summary>Parses the YAML stream and replaces <c>Data</c> with the flattened key/value pairs.</summary>
        /// <param name="stream">Open stream over the YAML file.</param>
        /// <exception cref="FormatException">The YAML is malformed or its root is not a mapping.</exception>
        public override void Load(Stream stream)
        {
            Data = Parse(stream);
        }

        /// <summary>Flattens a YAML document into configuration keys. An empty document yields no keys.</summary>
        /// <param name="stream">Stream containing YAML text.</param>
        /// <returns>Case-insensitive key/value pairs; null scalars are stored as <c>null</c>.</returns>
        /// <exception cref="FormatException">The YAML is malformed or its root is not a mapping.</exception>
        public static Dictionary<string, string?> Parse(Stream stream)
        {
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var yaml = new YamlStream();
            try
            {
                using var reader = new StreamReader(stream);
                yaml.Load(reader);
            }
            catch (YamlDotNet.Core.YamlException ex)
            {
                throw new FormatException($"Could not parse the YAML configuration: {ex.Message}", ex);
            }

            if (yaml.Documents.Count == 0)
            {
                return data;
            }

            if (yaml.Documents[0].RootNode is not YamlMappingNode root)
            {
                throw new FormatException("The YAML configuration root must be a mapping.");
            }

            Visit(root, null, data);
            return data;
        }

        /// <summary>Recursively walks a node, writing scalar leaves into <paramref name="data"/>.</summary>
        /// <param name="node">Node to visit.</param>
        /// <param name="path">Colon-delimited key of the node, or null for the root.</param>
        /// <param name="data">Destination dictionary.</param>
        private static void Visit(YamlNode node, string? path, Dictionary<string, string?> data)
        {
            switch (node)
            {
                case YamlMappingNode mapping:
                    foreach (var (key, value) in mapping.Children)
                    {
                        var name = ((YamlScalarNode)key).Value ?? string.Empty;
                        Visit(value, path == null ? name : $"{path}:{name}", data);
                    }

                    break;
                case YamlSequenceNode sequence:
                    for (var i = 0; i < sequence.Children.Count; i++)
                    {
                        Visit(sequence.Children[i], $"{path}:{i}", data);
                    }

                    break;
                case YamlScalarNode scalar:
                    // An unquoted empty value or "~" is a YAML null; keep it null like the JSON provider.
                    var isNull = scalar.Style == YamlDotNet.Core.ScalarStyle.Plain
                        && (string.IsNullOrEmpty(scalar.Value) || scalar.Value is "~" or "null");
                    data[path!] = isNull ? null : scalar.Value;
                    break;
            }
        }
    }
}

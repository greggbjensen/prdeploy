using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrDeploy.Api.Business.Stores;

/// <summary>Serializes deployment values as YAML for consistent storage across providers.</summary>
internal static class DeploymentValueSerialization
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>Serializes a deployment value to YAML.</summary>
    public static string Serialize<T>(T value) => Serializer.Serialize(value);

    /// <summary>Deserializes a YAML deployment value into the requested type.</summary>
    public static T Deserialize<T>(string value) => Deserializer.Deserialize<T>(value);
}

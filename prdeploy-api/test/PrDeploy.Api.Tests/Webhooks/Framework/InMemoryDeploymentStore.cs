using PrDeploy.Api.Business.Stores.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrDeploy.Api.Tests.Webhooks.Framework;

/// <summary>
/// Stores serialized deployment values and their revisions for webhook tests.
/// </summary>
/// <remarks>
/// This fake uses the same YAML serialization as the production deployment stores.
/// </remarks>
public class InMemoryDeploymentStore : IDeploymentStore
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)
        .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly Dictionary<string, List<string>> _deploymentValues = new();

    /// <summary>
    /// Gets the stored serialized values, keyed by their normalized path.
    /// </summary>
    public IReadOnlyDictionary<string, List<string>> DeploymentValues => _deploymentValues;

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string owner, string repo, string name) => Task.FromResult(Get<T>($"{owner}/{repo}/{name}", 0));

    /// <inheritdoc />
    public Task<T> GetHistoricalAsync<T>(string owner, string repo, string name, int historical) =>
        Task.FromResult(Get<T>($"{owner}/{repo}/{name}", historical));

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string owner, string name) => Task.FromResult(Get<T>($"{owner}/{name}", 0));

    /// <inheritdoc />
    public Task<T> GetAsync<T>(string name) => Task.FromResult(Get<T>(name, 0));

    /// <inheritdoc />
    public Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false) =>
        Set($"{owner}/{repo}/{name}", value);

    /// <inheritdoc />
    public Task SetAsync<T>(string owner, string name, T value, bool isSecure = false) =>
        Set($"{owner}/{name}", value);

    /// <inheritdoc />
    public Task SetAsync<T>(string name, T value, bool isSecure = false) => Set(name, value);

    /// <summary>
    /// Deserializes the requested value revision, returning the type default when it is unavailable.
    /// </summary>
    private T Get<T>(string key, int historical)
    {
        if (!_deploymentValues.TryGetValue(key.ToUpperInvariant(), out var history) || history.Count <= historical)
        {
            return default!;
        }

        return Deserializer.Deserialize<T>(history[history.Count - (historical + 1)]);
    }

    /// <summary>
    /// Appends a YAML-serialized value to its in-memory revision history.
    /// </summary>
    private Task Set<T>(string key, T value)
    {
        var upperKey = key.ToUpperInvariant();
        if (!_deploymentValues.TryGetValue(upperKey, out var history))
        {
            history = [];
            _deploymentValues[upperKey] = history;
        }

        history.Add(Serializer.Serialize(value));
        return Task.CompletedTask;
    }
}

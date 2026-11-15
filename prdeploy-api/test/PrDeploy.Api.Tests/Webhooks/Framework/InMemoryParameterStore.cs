using PrDeploy.Api.Business.Stores.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PrDeploy.Api.Tests.Webhooks.Framework;

/// <summary>
/// Stores YAML values with history, like AWS Parameter Store, using the same serialization as ParameterStore.
/// </summary>
public class InMemoryParameterStore : IParameterStore
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)
        .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly Dictionary<string, List<string>> _parameters = new();

    public IReadOnlyDictionary<string, List<string>> Parameters => _parameters;

    public Task<T> GetAsync<T>(string owner, string repo, string name) => Task.FromResult(Get<T>($"{owner}/{repo}/{name}", 0));

    public Task<T> GetHistoricalAsync<T>(string owner, string repo, string name, int historical) =>
        Task.FromResult(Get<T>($"{owner}/{repo}/{name}", historical));

    public Task<T> GetAsync<T>(string owner, string name) => Task.FromResult(Get<T>($"{owner}/{name}", 0));

    public Task<T> GetAsync<T>(string name) => Task.FromResult(Get<T>(name, 0));

    public Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false) =>
        Set($"{owner}/{repo}/{name}", value);

    public Task SetAsync<T>(string owner, string name, T value, bool isSecure = false) =>
        Set($"{owner}/{name}", value);

    public Task SetAsync<T>(string name, T value, bool isSecure = false) => Set(name, value);

    private T Get<T>(string key, int historical)
    {
        if (!_parameters.TryGetValue(key.ToUpperInvariant(), out var history) || history.Count <= historical)
        {
            return default!;
        }

        return Deserializer.Deserialize<T>(history[history.Count - (historical + 1)]);
    }

    private Task Set<T>(string key, T value)
    {
        var upperKey = key.ToUpperInvariant();
        if (!_parameters.TryGetValue(upperKey, out var history))
        {
            history = [];
            _parameters[upperKey] = history;
        }

        history.Add(Serializer.Serialize(value));
        return Task.CompletedTask;
    }
}

namespace PrDeploy.Api.Business.Stores.Interfaces;

public interface IParameterStore
{
    Task<T> GetAsync<T>(string owner, string repo, string name);

    /// <summary>
    /// Gets a previous version of a repository parameter.
    /// </summary>
    /// <param name="historical">How many versions back to go, where 0 is the current value.</param>
    Task<T> GetHistoricalAsync<T>(string owner, string repo, string name, int historical);
    Task<T> GetAsync<T>(string owner, string name);
    Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false);
    Task SetAsync<T>(string owner, string name, T value, bool isSecure = false);
    Task<T> GetAsync<T>(string name);
    Task SetAsync<T>(string name, T value, bool isSecure = false);
}
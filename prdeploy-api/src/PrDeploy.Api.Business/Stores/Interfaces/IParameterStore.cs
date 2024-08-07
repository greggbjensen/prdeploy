namespace PrDeploy.Api.Business.Stores.Interfaces;

public interface IParameterStore
{
    Task<T> GetAsync<T>(string owner, string repo, string name);
    Task<T> GetAsync<T>(string owner, string name);
    Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false);
    Task SetAsync<T>(string owner, string name, T value, bool isSecure = false);
    Task<T> GetAsync<T>(string name);
    Task SetAsync<T>(string name, T value, bool isSecure = false);
}
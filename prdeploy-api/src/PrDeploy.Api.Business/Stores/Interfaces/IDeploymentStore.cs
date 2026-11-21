namespace PrDeploy.Api.Business.Stores.Interfaces;

/// <summary>Reads and writes deployment configuration values and their historical revisions.</summary>
public interface IDeploymentStore
{
    /// <summary>Retrieves the requested value.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="repo">Repository whose values are addressed.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <typeparam name="T">Type of value consumed or produced by this operation.</typeparam>
    /// <returns>Requested value, or its default when no value is available.</returns>
    Task<T> GetAsync<T>(string owner, string repo, string name);

    /// <summary>Gets a previous revision of a repository deployment value.</summary>
    /// <param name="owner">Repository owner or organization name.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="name">Deployment-value name.</param>
    /// <param name="historical">How many revisions back to go, where 0 is the current value.</param>
    /// <returns>The requested deserialized value, or the type's default value when that revision is absent.</returns>
    Task<T> GetHistoricalAsync<T>(string owner, string repo, string name, int historical);

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="owner">Repository owner or organization whose values are addressed.</param>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <typeparam name="T">Type of value consumed or produced by this operation.</typeparam>
    /// <returns>Requested value, or its default when no value is available.</returns>
    Task<T> GetAsync<T>(string owner, string name);

    /// <summary>Appends a value revision scoped to a repository.</summary>
    /// <param name="owner">Repository owner or organization name.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="name">Deployment-value name.</param>
    /// <param name="value">Value to serialize and store.</param>
    /// <param name="isSecure">Metadata flag indicating that the caller considers the value sensitive.</param>
    Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false);

    /// <summary>Appends a value revision scoped to an owner.</summary>
    /// <param name="owner">Owner or organization name.</param>
    /// <param name="name">Deployment-value name.</param>
    /// <param name="value">Value to serialize and store.</param>
    /// <param name="isSecure">Metadata flag indicating that the caller considers the value sensitive.</param>
    Task SetAsync<T>(string owner, string name, T value, bool isSecure = false);

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="name">Name used to identify the value or resource.</param>
    /// <typeparam name="T">Type of value consumed or produced by this operation.</typeparam>
    /// <returns>Requested value, or its default when no value is available.</returns>
    Task<T> GetAsync<T>(string name);

    /// <summary>Appends a global value revision.</summary>
    /// <param name="name">Deployment-value name.</param>
    /// <param name="value">Value to serialize and store.</param>
    /// <param name="isSecure">Metadata flag indicating that the caller considers the value sensitive.</param>
    Task SetAsync<T>(string name, T value, bool isSecure = false);
}
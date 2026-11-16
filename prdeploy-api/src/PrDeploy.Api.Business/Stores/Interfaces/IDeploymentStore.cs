namespace PrDeploy.Api.Business.Stores.Interfaces;

/// <summary>
/// Reads and writes deployment configuration values and their historical revisions.
/// </summary>
public interface IDeploymentStore
{
    /// <summary>
    /// Gets the latest value scoped to an owner and repository, or the default value when it is absent.
    /// </summary>
    /// <param name="owner">Repository owner or organization name.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="name">Deployment-value name.</param>
    /// <returns>The deserialized latest value, or the type's default value when no revision exists.</returns>
    Task<T> GetAsync<T>(string owner, string repo, string name);

    /// <summary>
    /// Gets a previous revision of a repository deployment value.
    /// </summary>
    /// <param name="owner">Repository owner or organization name.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="name">Deployment-value name.</param>
    /// <param name="historical">How many revisions back to go, where 0 is the current value.</param>
    /// <returns>The requested deserialized value, or the type's default value when that revision is absent.</returns>
    Task<T> GetHistoricalAsync<T>(string owner, string repo, string name, int historical);

    /// <summary>
    /// Gets the latest value scoped to an owner, or the default value when it is absent.
    /// </summary>
    /// <param name="owner">Owner or organization name.</param>
    /// <param name="name">Deployment-value name.</param>
    /// <returns>The deserialized latest value, or the type's default value when no revision exists.</returns>
    Task<T> GetAsync<T>(string owner, string name);

    /// <summary>
    /// Appends a value revision scoped to a repository.
    /// </summary>
    /// <param name="owner">Repository owner or organization name.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="name">Deployment-value name.</param>
    /// <param name="value">Value to serialize and store.</param>
    /// <param name="isSecure">Metadata flag indicating that the caller considers the value sensitive.</param>
    Task SetAsync<T>(string owner, string repo, string name, T value, bool isSecure = false);

    /// <summary>
    /// Appends a value revision scoped to an owner.
    /// </summary>
    /// <param name="owner">Owner or organization name.</param>
    /// <param name="name">Deployment-value name.</param>
    /// <param name="value">Value to serialize and store.</param>
    /// <param name="isSecure">Metadata flag indicating that the caller considers the value sensitive.</param>
    Task SetAsync<T>(string owner, string name, T value, bool isSecure = false);

    /// <summary>
    /// Gets the latest global value, or the default value when it is absent.
    /// </summary>
    /// <param name="name">Deployment-value name.</param>
    /// <returns>The deserialized latest value, or the type's default value when no revision exists.</returns>
    Task<T> GetAsync<T>(string name);

    /// <summary>
    /// Appends a global value revision.
    /// </summary>
    /// <param name="name">Deployment-value name.</param>
    /// <param name="value">Value to serialize and store.</param>
    /// <param name="isSecure">Metadata flag indicating that the caller considers the value sensitive.</param>
    Task SetAsync<T>(string name, T value, bool isSecure = false);
}
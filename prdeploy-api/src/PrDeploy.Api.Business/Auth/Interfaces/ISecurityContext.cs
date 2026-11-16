namespace PrDeploy.Api.Business.Auth.Interfaces;

/// <summary>
/// Defines the contract for security Context operations.
/// </summary>
public interface ISecurityContext
{
    /// <summary>
    /// Gets the user token.
    /// </summary>
    /// <value>Current user token value.</value>
    string UserToken { get; }
    /// <summary>
    /// Gets the login.
    /// </summary>
    /// <value>Current login value.</value>
    string Login { get; }
}
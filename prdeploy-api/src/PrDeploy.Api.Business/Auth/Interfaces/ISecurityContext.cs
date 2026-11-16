namespace PrDeploy.Api.Business.Auth.Interfaces;

/// <summary>
/// Defines the contract for security Context operations.
/// </summary>
public interface ISecurityContext
{
    /// <summary>
    /// Gets the User Token.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    string UserToken { get; }
    /// <summary>
    /// Gets the Login.
    /// </summary>
    /// <returns>The value exposed by this member.</returns>
    string Login { get; }
}
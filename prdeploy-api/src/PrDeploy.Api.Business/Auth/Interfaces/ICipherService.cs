namespace PrDeploy.Api.Business.Auth.Interfaces;

/// <summary>
/// Coordinates operations for the i Cipher Service and its collaborators.
/// </summary>
public interface ICipherService
{
    /// <summary>
    /// Encrypts the supplied data before it is stored or transmitted.
    /// </summary>
    /// <param name="input">Input object containing the operation's arguments.</param>
    /// <returns>The value produced by this operation.</returns>
    string Encrypt(string input);
    /// <summary>
    /// Decrypts the supplied data for application use.
    /// </summary>
    /// <param name="cipherText">Value supplied for cipher Text.</param>
    /// <returns>The value produced by this operation.</returns>
    string Decrypt(string cipherText);
}
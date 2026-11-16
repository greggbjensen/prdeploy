namespace PrDeploy.Api.Business.Auth.Interfaces;

/// <summary>
/// Coordinates i cipher operations and their collaborators.
/// </summary>
public interface ICipherService
{
    /// <summary>
    /// Encrypts the supplied data before it is stored or transmitted.
    /// </summary>
    /// <param name="input">Plaintext text to encrypt with the configured data protector.</param>
    /// <returns>The protected representation of the plaintext input.</returns>
    string Encrypt(string input);
    /// <summary>
    /// Decrypts the supplied data for application use.
    /// </summary>
    /// <param name="cipherText">Encrypted text to decrypt with the configured data protector.</param>
    /// <returns>The original plaintext represented by the ciphertext.</returns>
    string Decrypt(string cipherText);
}
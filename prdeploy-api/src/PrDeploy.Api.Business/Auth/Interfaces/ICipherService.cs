namespace PrDeploy.Api.Business.Auth.Interfaces;

public interface ICipherService
{
    string Encrypt(string input);
    string Decrypt(string cipherText);
}
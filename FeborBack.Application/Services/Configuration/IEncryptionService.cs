namespace FeborBack.Application.Services.Configuration;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace FeborBack.Application.Services.Configuration;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IConfiguration configuration)
    {
        // Usar una clave de encriptación desde la configuración
        // En producción, esto debe estar en variables de entorno o Azure Key Vault
        var encryptionKey = configuration["Encryption:Key"] ?? "FeborCooperativa2025SecureKey!!"; // 32 caracteres
        var encryptionIV = configuration["Encryption:IV"] ?? "FeborSecureIV16!"; // 16 caracteres

        _key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
        _iv = Encoding.UTF8.GetBytes(encryptionIV.PadRight(16).Substring(0, 16));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en Encrypt: {ex.Message}");
            throw new Exception("Error al encriptar la contraseña", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en Decrypt: {ex.Message}");
            throw new Exception("Error al desencriptar la contraseña", ex);
        }
    }
}

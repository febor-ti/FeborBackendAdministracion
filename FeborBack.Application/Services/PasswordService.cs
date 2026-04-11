using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace FeborBack.Application.Services;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password, out string salt)
    {
        salt = BCrypt.Net.BCrypt.GenerateSalt(12);
        return BCrypt.Net.BCrypt.HashPassword(password, salt);
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    public string GenerateRandomPassword(int length = 12)
    {
        const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        var chars = new char[length];

        for (int i = 0; i < length; i++)
        {
            chars[i] = validChars[random.Next(validChars.Length)];
        }

        return new string(chars);
    }

    public bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        // Al menos una mayúscula, una minúscula, un número y un carácter especial
        var hasUpperCase = Regex.IsMatch(password, @"[A-Z]");
        var hasLowerCase = Regex.IsMatch(password, @"[a-z]");
        var hasNumber = Regex.IsMatch(password, @"\d");
        var hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");

        return hasUpperCase && hasLowerCase && hasNumber && hasSpecialChar;
    }
}
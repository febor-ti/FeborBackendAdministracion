namespace FeborBack.Application.Services;

public interface IPasswordService
{
    string HashPassword(string password, out string salt);
    bool VerifyPassword(string password, string hash, string salt);
    string GenerateRandomPassword(int length = 12);
    bool IsPasswordStrong(string password);
}
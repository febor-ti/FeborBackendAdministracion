using FeborBack.Application.DTOs.Auth;

namespace FeborBack.Application.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<bool> LogoutAsync(string refreshToken);
    Task<bool> LogoutAllAsync(int userId);

    /// <summary>
    /// Registra un nuevo usuario. Si no se proporciona contraseña, genera una temporal automáticamente.
    /// Retorna el usuario creado y la contraseña temporal (si fue generada automáticamente).
    /// </summary>
    Task<(UserInfoDto User, string? TemporaryPassword)> RegisterUserAsync(RegisterUserDto request, int createdBy);

    Task<UserInfoDto?> GetUserInfoAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> ValidateTokenAsync(string token);

    Task<UserInfoDto> CreateInitialAdminAsync();
}
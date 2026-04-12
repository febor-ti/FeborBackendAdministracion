namespace FeborBack.Application.DTOs.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfoDto? User { get; set; }

    // 2FA: cuando es true, AccessToken/RefreshToken están vacíos y el cliente debe verificar el código
    public bool RequiresTwoFactor { get; set; } = false;
    public string? SessionToken { get; set; }
}
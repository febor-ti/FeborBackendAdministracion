namespace FeborBack.Application.DTOs.Auth;

public class VerifyTwoFactorDto
{
    public string SessionToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

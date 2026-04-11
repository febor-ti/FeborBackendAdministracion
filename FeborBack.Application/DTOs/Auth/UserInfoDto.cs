namespace FeborBack.Application.DTOs.Auth;

public class UserInfoDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarName { get; set; }
    public bool IsTemporaryPassword { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<int> RoleIds { get; set; } = new();
    public List<string> Claims { get; set; } = new();
}
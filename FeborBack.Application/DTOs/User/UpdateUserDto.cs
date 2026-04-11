namespace FeborBack.Application.DTOs.User;

public class UpdateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarName { get; set; }
    public string? Password { get; set; } // Opcional: solo se actualiza si se proporciona
    public List<int> RoleIds { get; set; } = new();
    public int StatusId { get; set; } = 1; // Active por defecto
    public int? StatusReasonId { get; set; }
}
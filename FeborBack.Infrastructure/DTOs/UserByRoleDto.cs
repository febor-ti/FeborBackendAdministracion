namespace FeborBack.Infrastructure.DTOs;

/// <summary>
/// DTO para mapear usuarios por rol desde la base de datos (snake_case)
/// </summary>
public class UserByRoleDbDto
{
    public int user_id { get; set; }
    public int person_id { get; set; }
    public string full_name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
    public bool is_active { get; set; }
    public DateTime created_at { get; set; }
    public DateTime role_assigned_at { get; set; }
}

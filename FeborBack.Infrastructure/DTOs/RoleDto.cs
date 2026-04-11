using FeborBack.Domain.Entities;

namespace FeborBack.Infrastructure.DTOs;

public class RoleDto
{
    public int role_id { get; set; }
    public string role_name { get; set; } = string.Empty;
    public string? description { get; set; }
    public bool is_active { get; set; }
    public DateTime created_at { get; set; }

    public Role ToRole()
    {
        return new Role
        {
            RoleId = role_id,
            RoleName = role_name,
            Description = description,
            IsActive = is_active,
            CreatedAt = created_at
        };
    }
}
using FeborBack.Domain.Entities.Menu;

namespace FeborBack.Infrastructure.DTOs.Menu;

/// <summary>
/// DTO para mapear desde la base de datos (snake_case)
/// </summary>
public class MenuItemDto
{
    public int menu_item_id { get; set; }
    public int? parent_id { get; set; }
    public string title { get; set; } = string.Empty;
    public string? menu_key { get; set; }
    public string? route_path { get; set; }
    public string? route_name { get; set; }
    public string? icon { get; set; }
    public string? heading { get; set; }
    public string? claim_action { get; set; }
    public string? claim_subject { get; set; }
    public int order_index { get; set; }
    public bool is_active { get; set; }
    public bool is_view { get; set; }
    public int created_by { get; set; }
    public DateTime created_at { get; set; }
    public int? updated_by { get; set; }
    public DateTime? updated_at { get; set; }

    public MenuItem ToMenuItem()
    {
        return new MenuItem
        {
            MenuItemId = menu_item_id,
            ParentId = parent_id,
            Title = title,
            MenuKey = menu_key,
            RoutePath = route_path,
            RouteName = route_name,
            Icon = icon,
            Heading = heading,
            ClaimAction = claim_action,
            ClaimSubject = claim_subject,
            OrderIndex = order_index,
            IsActive = is_active,
            IsView = is_view,
            CreatedBy = created_by,
            CreatedAt = created_at,
            UpdatedBy = updated_by,
            UpdatedAt = updated_at
        };
    }
}

/// <summary>
/// DTO simplificado para el menú del usuario
/// </summary>
public class UserMenuItemDto
{
    public int menu_item_id { get; set; }
    public int? parent_id { get; set; }
    public string title { get; set; } = string.Empty;
    public string? menu_key { get; set; }
    public string? route_path { get; set; }
    public string? route_name { get; set; }
    public string? icon { get; set; }
    public string? heading { get; set; }
    public string? claim_action { get; set; }
    public string? claim_subject { get; set; }
    public int order_index { get; set; }
    public bool is_view { get; set; }
}

/// <summary>
/// DTO para roles asignados a un menú
/// </summary>
public class MenuRoleDetailDto
{
    public int role_id { get; set; }
    public string role_name { get; set; } = string.Empty;
    public string? description { get; set; }
    public DateTime assigned_at { get; set; }
    public int assigned_by { get; set; }
    public string assigned_by_name { get; set; } = string.Empty;
}

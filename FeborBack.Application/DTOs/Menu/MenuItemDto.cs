using System.ComponentModel.DataAnnotations;

namespace FeborBack.Application.DTOs.Menu;

/// <summary>
/// DTO completo de un item de menú
/// </summary>
public class MenuItemDto
{
    public int MenuItemId { get; set; }
    public int? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? MenuKey { get; set; }
    public string? RoutePath { get; set; }
    public string? RouteName { get; set; }
    public string? Icon { get; set; }
    public string? Heading { get; set; }
    public string? ClaimAction { get; set; }
    public string? ClaimSubject { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; }
    public bool IsView { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Propiedad adicional para roles asignados (opcional)
    public List<MenuRoleDto>? AssignedRoles { get; set; }
}

/// <summary>
/// DTO para crear un nuevo item de menú
/// </summary>
public class CreateMenuItemDto
{
    public int? ParentId { get; set; }

    [Required(ErrorMessage = "El título es requerido")]
    [MaxLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "La clave del menú no puede exceder 100 caracteres")]
    public string? MenuKey { get; set; }

    [MaxLength(255, ErrorMessage = "La ruta no puede exceder 255 caracteres")]
    public string? RoutePath { get; set; }

    [MaxLength(100, ErrorMessage = "El nombre de ruta no puede exceder 100 caracteres")]
    public string? RouteName { get; set; }

    [MaxLength(100, ErrorMessage = "El icono no puede exceder 100 caracteres")]
    public string? Icon { get; set; }

    [MaxLength(100, ErrorMessage = "El encabezado no puede exceder 100 caracteres")]
    public string? Heading { get; set; }

    [MaxLength(50, ErrorMessage = "La acción del claim no puede exceder 50 caracteres")]
    public string? ClaimAction { get; set; }

    [MaxLength(50, ErrorMessage = "El sujeto del claim no puede exceder 50 caracteres")]
    public string? ClaimSubject { get; set; }

    public int OrderIndex { get; set; } = 0;

    public bool IsView { get; set; } = false;

    // IDs de roles a asignar al crear el menú
    public int[]? RoleIds { get; set; }
}

/// <summary>
/// DTO para actualizar un item de menú
/// </summary>
public class UpdateMenuItemDto
{
    [Required]
    public int MenuItemId { get; set; }

    public int? ParentId { get; set; }

    [Required(ErrorMessage = "El título es requerido")]
    [MaxLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "La clave del menú no puede exceder 100 caracteres")]
    public string? MenuKey { get; set; }

    [MaxLength(255, ErrorMessage = "La ruta no puede exceder 255 caracteres")]
    public string? RoutePath { get; set; }

    [MaxLength(100, ErrorMessage = "El nombre de ruta no puede exceder 100 caracteres")]
    public string? RouteName { get; set; }

    [MaxLength(100, ErrorMessage = "El icono no puede exceder 100 caracteres")]
    public string? Icon { get; set; }

    [MaxLength(100, ErrorMessage = "El encabezado no puede exceder 100 caracteres")]
    public string? Heading { get; set; }

    [MaxLength(50, ErrorMessage = "La acción del claim no puede exceder 50 caracteres")]
    public string? ClaimAction { get; set; }

    [MaxLength(50, ErrorMessage = "El sujeto del claim no puede exceder 50 caracteres")]
    public string? ClaimSubject { get; set; }

    public int OrderIndex { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public bool IsView { get; set; } = false;

    // IDs de roles a asignar (reemplaza los existentes)
    public int[]? RoleIds { get; set; }
}

/// <summary>
/// DTO para el menú del usuario (formato del frontend)
/// </summary>
public class UserMenuDto
{
    public string? Title { get; set; }
    public RouteInfo? To { get; set; }
    public IconInfo? Icon { get; set; }
    public string? Heading { get; set; }
    public string? Action { get; set; }
    public string? Subject { get; set; }
    public bool IsView { get; set; }
    public List<UserMenuDto>? Children { get; set; }
}

public class RouteInfo
{
    public string? Name { get; set; }
    public string? Path { get; set; }
}

public class IconInfo
{
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// DTO para roles asignados a un menú
/// </summary>
public class MenuRoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime AssignedAt { get; set; }
    public int AssignedBy { get; set; }
    public string AssignedByName { get; set; } = string.Empty;
}

/// <summary>
/// DTO para asignar roles a un menú
/// </summary>
public class AssignRolesToMenuDto
{
    [Required]
    public int MenuItemId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Debe asignar al menos un rol")]
    public int[] RoleIds { get; set; } = Array.Empty<int>();
}

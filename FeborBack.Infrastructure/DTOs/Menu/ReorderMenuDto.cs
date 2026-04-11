using System.ComponentModel.DataAnnotations;

namespace FeborBack.Infrastructure.DTOs.Menu;

/// <summary>
/// DTO para reordenar múltiples menús
/// </summary>
public class ReorderMenusDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Debe proporcionar al menos un item para reordenar")]
    public MenuOrderItem[] Items { get; set; } = Array.Empty<MenuOrderItem>();
}

/// <summary>
/// Item individual con su nueva posición
/// </summary>
public class MenuOrderItem
{
    [Required]
    public int MenuItemId { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "El orden debe ser mayor o igual a 0")]
    public int OrderIndex { get; set; }
}

/// <summary>
/// DTO para mover un menú a otro padre
/// </summary>
public class MoveMenuItemDto
{
    [Required]
    public int MenuItemId { get; set; }

    public int? NewParentId { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "El orden debe ser mayor o igual a 0")]
    public int NewOrderIndex { get; set; }
}

/// <summary>
/// DTO para intercambiar la posición de dos menús
/// </summary>
public class SwapMenuOrderDto
{
    [Required]
    public int MenuItemId1 { get; set; }

    [Required]
    public int MenuItemId2 { get; set; }
}

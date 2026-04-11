using FeborBack.Application.DTOs.Menu;
using InfraMenu = FeborBack.Infrastructure.DTOs.Menu;

namespace FeborBack.Application.Services.Menu;

public interface IMenuService
{
    // CRUD de items de menú
    Task<int> CreateMenuItemAsync(CreateMenuItemDto dto, int createdBy);
    Task UpdateMenuItemAsync(UpdateMenuItemDto dto, int updatedBy);
    Task DeleteMenuItemAsync(int menuItemId, int updatedBy);
    Task<MenuItemDto?> GetMenuItemByIdAsync(int menuItemId);
    Task<IEnumerable<MenuItemDto>> GetAllMenuItemsAsync(bool includeInactive = false);
    Task<IEnumerable<MenuItemDto>> GetRootMenuItemsAsync(bool includeInactive = false);
    Task<IEnumerable<MenuItemDto>> GetMenuChildrenAsync(int parentId);

    // Obtener menú para el frontend
    Task<List<UserMenuDto>> GetMenuByUserIdAsync(int userId);
    Task<List<UserMenuDto>> GetMenuByRoleIdAsync(int roleId);
    Task<bool> UserHasMenuAccessAsync(int userId, string menuKey);

    // Gestión de roles en menú
    Task AssignRolesToMenuAsync(int menuItemId, int[] roleIds, int assignedBy);
    Task<IEnumerable<MenuRoleDto>> GetRolesByMenuItemAsync(int menuItemId);

    // Reordenamiento de menús
    Task ReorderMenusAsync(InfraMenu.ReorderMenusDto dto, int updatedBy);
    Task MoveMenuItemAsync(InfraMenu.MoveMenuItemDto dto, int updatedBy);
    Task SwapMenuOrderAsync(InfraMenu.SwapMenuOrderDto dto, int updatedBy);
    Task ReindexSiblingsAsync(int? parentId, int updatedBy);
}

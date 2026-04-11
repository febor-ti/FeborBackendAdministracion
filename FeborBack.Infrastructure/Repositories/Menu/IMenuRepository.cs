using FeborBack.Domain.Entities.Menu;
using FeborBack.Infrastructure.DTOs.Menu;

namespace FeborBack.Infrastructure.Repositories.Menu;

public interface IMenuRepository
{
    // CRUD básico de items de menú
    Task<int> CreateMenuItemAsync(MenuItem menuItem);
    Task UpdateMenuItemAsync(MenuItem menuItem);
    Task DeleteMenuItemAsync(int menuItemId, int updatedBy);
    Task HardDeleteMenuItemAsync(int menuItemId);
    Task<MenuItem?> GetMenuItemByIdAsync(int menuItemId);
    Task<IEnumerable<MenuItem>> GetAllMenuItemsAsync(bool includeInactive = false);
    Task<IEnumerable<MenuItem>> GetRootMenuItemsAsync(bool includeInactive = false);
    Task<IEnumerable<MenuItem>> GetMenuChildrenAsync(int parentId);

    // Obtener menú para usuarios
    Task<IEnumerable<UserMenuItemDto>> GetMenuByRoleIdAsync(int roleId);
    Task<IEnumerable<UserMenuItemDto>> GetMenuByUserIdAsync(int userId);
    Task<bool> UserHasMenuAccessAsync(int userId, string menuKey);

    // Gestión de roles en menú
    Task<int> AssignMenuToRoleAsync(int menuItemId, int roleId, int assignedBy);
    Task AssignRolesToMenuAsync(int menuItemId, int[] roleIds, int assignedBy);
    Task RemoveRoleFromMenuAsync(int menuItemId, int roleId);
    Task<IEnumerable<MenuRoleDetailDto>> GetRolesByMenuItemAsync(int menuItemId);

    // Reordenamiento de menús
    Task ReorderMenusAsync(MenuOrderItem[] items, int updatedBy);
    Task MoveMenuItemAsync(int menuItemId, int? newParentId, int newOrderIndex, int updatedBy);
    Task SwapMenuOrderAsync(int menuItemId1, int menuItemId2, int updatedBy);
    Task ReindexSiblingsAsync(int? parentId, int updatedBy);
}

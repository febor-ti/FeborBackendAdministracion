using Microsoft.Extensions.Configuration;
using FeborBack.Domain.Entities.Menu;
using FeborBack.Infrastructure.DTOs.Menu;
using System.Text.Json;

namespace FeborBack.Infrastructure.Repositories.Menu;

public class MenuRepository : BaseRepository, IMenuRepository
{
    public MenuRepository(IConfiguration configuration)
        : base(configuration.GetConnectionString("DefaultConnection")
               ?? throw new ArgumentNullException("ConnectionString no configurado"))
    {
    }

    // ==========================================
    // CRUD básico de items de menú
    // ==========================================
    public async Task<int> CreateMenuItemAsync(MenuItem menuItem)
    {
        var result = await CallFunction<int>(
            "menu.sp_create_menu_item",
            new
            {
                p_parent_id = menuItem.ParentId,
                p_title = menuItem.Title,
                p_menu_key = menuItem.MenuKey,
                p_route_path = menuItem.RoutePath,
                p_route_name = menuItem.RouteName,
                p_icon = menuItem.Icon,
                p_heading = menuItem.Heading,
                p_claim_action = menuItem.ClaimAction,
                p_claim_subject = menuItem.ClaimSubject,
                p_order_index = menuItem.OrderIndex,
                p_created_by = menuItem.CreatedBy
            });

        return result;
    }

    public async Task UpdateMenuItemAsync(MenuItem menuItem)
    {
        await ExecuteFunction(
            "menu.sp_update_menu_item",
            new
            {
                p_menu_item_id = menuItem.MenuItemId,
                p_parent_id = menuItem.ParentId,
                p_title = menuItem.Title,
                p_menu_key = menuItem.MenuKey,
                p_route_path = menuItem.RoutePath,
                p_route_name = menuItem.RouteName,
                p_icon = menuItem.Icon,
                p_heading = menuItem.Heading,
                p_claim_action = menuItem.ClaimAction,
                p_claim_subject = menuItem.ClaimSubject,
                p_order_index = menuItem.OrderIndex,
                p_is_active = menuItem.IsActive,
                p_updated_by = menuItem.UpdatedBy
            });
    }

    public async Task DeleteMenuItemAsync(int menuItemId, int updatedBy)
    {
        await ExecuteFunction(
            "menu.sp_delete_menu_item",
            new { p_menu_item_id = menuItemId, p_updated_by = updatedBy });
    }

    public async Task HardDeleteMenuItemAsync(int menuItemId)
    {
        await ExecuteFunction(
            "menu.sp_hard_delete_menu_item",
            new { p_menu_item_id = menuItemId });
    }

    public async Task<MenuItem?> GetMenuItemByIdAsync(int menuItemId)
    {
        var menuItemDto = await CallFunction<MenuItemDto>(
            "menu.sp_get_menu_item_by_id",
            new { p_menu_item_id = menuItemId });

        return menuItemDto?.ToMenuItem();
    }

    public async Task<IEnumerable<MenuItem>> GetAllMenuItemsAsync(bool includeInactive = false)
    {
        var menuItemDtos = await CallTableFunction<MenuItemDto>(
            "menu.sp_get_all_menu_items",
            new { p_include_inactive = includeInactive });

        return menuItemDtos.Select(dto => dto.ToMenuItem());
    }

    public async Task<IEnumerable<MenuItem>> GetRootMenuItemsAsync(bool includeInactive = false)
    {
        var menuItemDtos = await CallTableFunction<MenuItemDto>(
            "menu.sp_get_root_menu_items",
            new { p_include_inactive = includeInactive });

        return menuItemDtos.Select(dto => dto.ToMenuItem());
    }

    public async Task<IEnumerable<MenuItem>> GetMenuChildrenAsync(int parentId)
    {
        var menuItemDtos = await CallTableFunction<MenuItemDto>(
            "menu.sp_get_menu_children",
            new { p_parent_id = parentId });

        return menuItemDtos.Select(dto => dto.ToMenuItem());
    }

    // ==========================================
    // Obtener menú para usuarios
    // ==========================================
    public async Task<IEnumerable<UserMenuItemDto>> GetMenuByRoleIdAsync(int roleId)
    {
        return await CallTableFunction<UserMenuItemDto>(
            "menu.sp_get_menu_by_role_id",
            new { p_role_id = roleId });
    }

    public async Task<IEnumerable<UserMenuItemDto>> GetMenuByUserIdAsync(int userId)
    {
        return await CallTableFunction<UserMenuItemDto>(
            "menu.sp_get_menu_by_user_id",
            new { p_user_id = userId });
    }

    public async Task<bool> UserHasMenuAccessAsync(int userId, string menuKey)
    {
        var result = await CallFunction<bool>(
            "menu.sp_user_has_menu_access",
            new { p_user_id = userId, p_menu_key = menuKey });

        return result;
    }

    // ==========================================
    // Gestión de roles en menú
    // ==========================================
    public async Task<int> AssignMenuToRoleAsync(int menuItemId, int roleId, int assignedBy)
    {
        var result = await CallFunction<int>(
            "menu.sp_assign_menu_to_role",
            new
            {
                p_menu_item_id = menuItemId,
                p_role_id = roleId,
                p_assigned_by = assignedBy
            });

        return result;
    }

    public async Task AssignRolesToMenuAsync(int menuItemId, int[] roleIds, int assignedBy)
    {
        await ExecuteFunction(
            "menu.sp_assign_roles_to_menu",
            new
            {
                p_menu_item_id = menuItemId,
                p_role_ids = roleIds,
                p_assigned_by = assignedBy
            });
    }

    public async Task RemoveRoleFromMenuAsync(int menuItemId, int roleId)
    {
        await ExecuteFunction(
            "menu.sp_remove_role_from_menu",
            new { p_menu_item_id = menuItemId, p_role_id = roleId });
    }

    public async Task<IEnumerable<MenuRoleDetailDto>> GetRolesByMenuItemAsync(int menuItemId)
    {
        return await CallTableFunction<MenuRoleDetailDto>(
            "menu.sp_get_roles_by_menu_item",
            new { p_menu_item_id = menuItemId });
    }

    // ==========================================
    // Reordenamiento de menús
    // ==========================================
    public async Task ReorderMenusAsync(MenuOrderItem[] items, int updatedBy)
    {
        // Convertir el array a JSON para PostgreSQL
        var jsonArray = JsonSerializer.Serialize(items.Select(i => new
        {
            menuItemId = i.MenuItemId,
            orderIndex = i.OrderIndex
        }));

        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new Npgsql.NpgsqlCommand("SELECT menu.sp_reorder_menus(@p_menu_orders, @p_updated_by)", connection);

        var jsonbParam = new Npgsql.NpgsqlParameter("@p_menu_orders", NpgsqlTypes.NpgsqlDbType.Jsonb)
        {
            Value = jsonArray
        };
        command.Parameters.Add(jsonbParam);
        command.Parameters.AddWithValue("@p_updated_by", updatedBy);

        await command.ExecuteNonQueryAsync();
    }

    public async Task MoveMenuItemAsync(int menuItemId, int? newParentId, int newOrderIndex, int updatedBy)
    {
        await ExecuteFunction(
            "menu.sp_move_menu_item",
            new
            {
                p_menu_item_id = menuItemId,
                p_new_parent_id = newParentId,
                p_new_order_index = newOrderIndex,
                p_updated_by = updatedBy
            });
    }

    public async Task SwapMenuOrderAsync(int menuItemId1, int menuItemId2, int updatedBy)
    {
        await ExecuteFunction(
            "menu.sp_swap_menu_order",
            new
            {
                p_menu_item_id_1 = menuItemId1,
                p_menu_item_id_2 = menuItemId2,
                p_updated_by = updatedBy
            });
    }

    public async Task ReindexSiblingsAsync(int? parentId, int updatedBy)
    {
        await ExecuteFunction(
            "menu.sp_reindex_siblings",
            new { p_parent_id = parentId, p_updated_by = updatedBy });
    }
}

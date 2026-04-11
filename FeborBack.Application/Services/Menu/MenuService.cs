using AutoMapper;
using FeborBack.Application.DTOs.Menu;
using FeborBack.Domain.Entities.Menu;
using FeborBack.Infrastructure.Repositories.Menu;
using InfraMenu = FeborBack.Infrastructure.DTOs.Menu;

namespace FeborBack.Application.Services.Menu;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _menuRepository;
    private readonly IMapper _mapper;

    public MenuService(IMenuRepository menuRepository, IMapper mapper)
    {
        _menuRepository = menuRepository ?? throw new ArgumentNullException(nameof(menuRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    // ==========================================
    // CRUD de items de menú
    // ==========================================
    public async Task<int> CreateMenuItemAsync(CreateMenuItemDto dto, int createdBy)
    {
        var menuItem = new MenuItem
        {
            ParentId = dto.ParentId,
            Title = dto.Title,
            MenuKey = dto.MenuKey,
            RoutePath = dto.RoutePath,
            RouteName = dto.RouteName,
            Icon = dto.Icon,
            Heading = dto.Heading,
            ClaimAction = dto.ClaimAction,
            ClaimSubject = dto.ClaimSubject,
            OrderIndex = dto.OrderIndex,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var menuItemId = await _menuRepository.CreateMenuItemAsync(menuItem);

        // Si se proporcionaron roles, asignarlos
        if (dto.RoleIds != null && dto.RoleIds.Length > 0)
        {
            await _menuRepository.AssignRolesToMenuAsync(menuItemId, dto.RoleIds, createdBy);
        }

        return menuItemId;
    }

    public async Task UpdateMenuItemAsync(UpdateMenuItemDto dto, int updatedBy)
    {
        var menuItem = new MenuItem
        {
            MenuItemId = dto.MenuItemId,
            ParentId = dto.ParentId,
            Title = dto.Title,
            MenuKey = dto.MenuKey,
            RoutePath = dto.RoutePath,
            RouteName = dto.RouteName,
            Icon = dto.Icon,
            Heading = dto.Heading,
            ClaimAction = dto.ClaimAction,
            ClaimSubject = dto.ClaimSubject,
            OrderIndex = dto.OrderIndex,
            IsActive = dto.IsActive,
            UpdatedBy = updatedBy,
            UpdatedAt = DateTime.UtcNow
        };

        await _menuRepository.UpdateMenuItemAsync(menuItem);

        // Si se proporcionaron roles, actualizarlos
        if (dto.RoleIds != null)
        {
            await _menuRepository.AssignRolesToMenuAsync(dto.MenuItemId, dto.RoleIds, updatedBy);
        }
    }

    public async Task DeleteMenuItemAsync(int menuItemId, int updatedBy)
    {
        await _menuRepository.DeleteMenuItemAsync(menuItemId, updatedBy);
    }

    public async Task<MenuItemDto?> GetMenuItemByIdAsync(int menuItemId)
    {
        var menuItem = await _menuRepository.GetMenuItemByIdAsync(menuItemId);
        if (menuItem == null)
            return null;

        var menuItemDto = _mapper.Map<MenuItemDto>(menuItem);

        // Obtener roles asignados
        var roles = await _menuRepository.GetRolesByMenuItemAsync(menuItemId);
        menuItemDto.AssignedRoles = roles.Select(r => new MenuRoleDto
        {
            RoleId = r.role_id,
            RoleName = r.role_name,
            Description = r.description,
            AssignedAt = r.assigned_at,
            AssignedBy = r.assigned_by,
            AssignedByName = r.assigned_by_name
        }).ToList();

        return menuItemDto;
    }

    public async Task<IEnumerable<MenuItemDto>> GetAllMenuItemsAsync(bool includeInactive = false)
    {
        var menuItems = await _menuRepository.GetAllMenuItemsAsync(includeInactive);
        return _mapper.Map<IEnumerable<MenuItemDto>>(menuItems);
    }

    public async Task<IEnumerable<MenuItemDto>> GetRootMenuItemsAsync(bool includeInactive = false)
    {
        var menuItems = await _menuRepository.GetRootMenuItemsAsync(includeInactive);
        return _mapper.Map<IEnumerable<MenuItemDto>>(menuItems);
    }

    public async Task<IEnumerable<MenuItemDto>> GetMenuChildrenAsync(int parentId)
    {
        var menuItems = await _menuRepository.GetMenuChildrenAsync(parentId);
        return _mapper.Map<IEnumerable<MenuItemDto>>(menuItems);
    }

    // ==========================================
    // Obtener menú para el frontend
    // ==========================================
    public async Task<List<UserMenuDto>> GetMenuByUserIdAsync(int userId)
    {
        var menuItems = await _menuRepository.GetMenuByUserIdAsync(userId);
        return BuildMenuHierarchy(menuItems);
    }

    public async Task<List<UserMenuDto>> GetMenuByRoleIdAsync(int roleId)
    {
        var menuItems = await _menuRepository.GetMenuByRoleIdAsync(roleId);
        return BuildMenuHierarchy(menuItems);
    }

    public async Task<bool> UserHasMenuAccessAsync(int userId, string menuKey)
    {
        return await _menuRepository.UserHasMenuAccessAsync(userId, menuKey);
    }

    // ==========================================
    // Gestión de roles en menú
    // ==========================================
    public async Task AssignRolesToMenuAsync(int menuItemId, int[] roleIds, int assignedBy)
    {
        await _menuRepository.AssignRolesToMenuAsync(menuItemId, roleIds, assignedBy);
    }

    public async Task<IEnumerable<MenuRoleDto>> GetRolesByMenuItemAsync(int menuItemId)
    {
        var roles = await _menuRepository.GetRolesByMenuItemAsync(menuItemId);
        return roles.Select(r => new MenuRoleDto
        {
            RoleId = r.role_id,
            RoleName = r.role_name,
            Description = r.description,
            AssignedAt = r.assigned_at,
            AssignedBy = r.assigned_by,
            AssignedByName = r.assigned_by_name
        });
    }

    // ==========================================
    // Reordenamiento de menús
    // ==========================================
    public async Task ReorderMenusAsync(InfraMenu.ReorderMenusDto dto, int updatedBy)
    {
        await _menuRepository.ReorderMenusAsync(dto.Items, updatedBy);
    }

    public async Task MoveMenuItemAsync(InfraMenu.MoveMenuItemDto dto, int updatedBy)
    {
        await _menuRepository.MoveMenuItemAsync(
            dto.MenuItemId,
            dto.NewParentId,
            dto.NewOrderIndex,
            updatedBy);
    }

    public async Task SwapMenuOrderAsync(InfraMenu.SwapMenuOrderDto dto, int updatedBy)
    {
        await _menuRepository.SwapMenuOrderAsync(dto.MenuItemId1, dto.MenuItemId2, updatedBy);
    }

    public async Task ReindexSiblingsAsync(int? parentId, int updatedBy)
    {
        await _menuRepository.ReindexSiblingsAsync(parentId, updatedBy);
    }

    // ==========================================
    // Métodos privados
    // ==========================================
    private List<UserMenuDto> BuildMenuHierarchy(IEnumerable<InfraMenu.UserMenuItemDto> menuItems)
    {
        var menuList = menuItems.ToList();
        var result = new List<UserMenuDto>();

        // Primero procesar items sin padre (raíz) y encabezados
        var rootItems = menuList.Where(m => m.parent_id == null).OrderBy(m => m.order_index);

        foreach (var item in rootItems)
        {
            // Si es un encabezado (heading)
            if (!string.IsNullOrWhiteSpace(item.heading))
            {
                result.Add(new UserMenuDto
                {
                    Heading = item.heading,
                    Action = item.claim_action,
                    Subject = item.claim_subject
                });
            }
            // Si es un item normal
            else
            {
                var menuDto = ConvertToUserMenuDto(item);

                // Buscar hijos recursivamente
                menuDto.Children = GetChildren(item.menu_item_id, menuList);

                result.Add(menuDto);
            }
        }

        return result;
    }

    private List<UserMenuDto>? GetChildren(int parentId, List<InfraMenu.UserMenuItemDto> allItems)
    {
        var children = allItems
            .Where(m => m.parent_id == parentId)
            .OrderBy(m => m.order_index)
            .Select(item =>
            {
                var menuDto = ConvertToUserMenuDto(item);
                menuDto.Children = GetChildren(item.menu_item_id, allItems);
                return menuDto;
            })
            .ToList();

        return children.Any() ? children : null;
    }

    private UserMenuDto ConvertToUserMenuDto(InfraMenu.UserMenuItemDto item)
    {
        var menuDto = new UserMenuDto
        {
            Title = item.title,
            Action = item.claim_action,
            Subject = item.claim_subject,
            IsView = item.is_view
        };

        // Configurar ícono si existe
        if (!string.IsNullOrWhiteSpace(item.icon))
        {
            menuDto.Icon = new IconInfo { Icon = item.icon };
        }

        // Configurar ruta si existe
        if (!string.IsNullOrWhiteSpace(item.route_name) || !string.IsNullOrWhiteSpace(item.route_path))
        {
            menuDto.To = new RouteInfo
            {
                Name = item.route_name,
                Path = item.route_path
            };
        }

        return menuDto;
    }
}

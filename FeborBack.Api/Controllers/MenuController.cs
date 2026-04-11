using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FeborBack.Application.Services.Menu;
using FeborBack.Application.DTOs.Menu;
using FeborBack.Api.Authorization;
using InfraMenu = FeborBack.Infrastructure.DTOs.Menu;

namespace FeborBack.Api.Controllers;

/// <summary>
/// Controlador para gestión administrativa de menús
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Solo requiere autenticación, permisos verificados por [MenuAuthorize] en cada endpoint
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ILogger<MenuController> _logger;

    public MenuController(IMenuService menuService, ILogger<MenuController> logger)
    {
        _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtener todos los items de menú
    /// </summary>
    [HttpGet]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> GetAllMenuItems([FromQuery] bool includeInactive = false)
    {
        try
        {
            var menuItems = await _menuService.GetAllMenuItemsAsync(includeInactive);
            return Ok(new { success = true, data = menuItems });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los items de menú");
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtener items raíz del menú
    /// </summary>
    [HttpGet("root")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> GetRootMenuItems([FromQuery] bool includeInactive = false)
    {
        try
        {
            var menuItems = await _menuService.GetRootMenuItemsAsync(includeInactive);
            return Ok(new { success = true, data = menuItems });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener items raíz del menú");
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtener un item de menú por ID
    /// </summary>
    [HttpGet("{id:int}")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> GetMenuItemById(int id)
    {
        try
        {
            var menuItem = await _menuService.GetMenuItemByIdAsync(id);

            if (menuItem == null)
                return NotFound(new { success = false, message = $"No se encontró el item de menú con ID {id}" });

            return Ok(new { success = true, data = menuItem });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el item de menú con ID {MenuItemId}", id);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtener items hijos de un menú
    /// </summary>
    [HttpGet("{id:int}/children")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> GetMenuChildren(int id)
    {
        try
        {
            var children = await _menuService.GetMenuChildrenAsync(id);
            return Ok(new { success = true, data = children });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener hijos del item de menú {MenuItemId}", id);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Crear un nuevo item de menú
    /// </summary>
    [HttpPost]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> CreateMenuItem([FromBody] CreateMenuItemDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });

            var menuItemId = await _menuService.CreateMenuItemAsync(dto, userId);

            return CreatedAtAction(
                nameof(GetMenuItemById),
                new { id = menuItemId },
                new { success = true, message = "Item de menú creado exitosamente", data = new { menuItemId } }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el item de menú");
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Actualizar un item de menú
    /// </summary>
    [HttpPut("{id:int}")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> UpdateMenuItem(int id, [FromBody] UpdateMenuItemDto dto)
    {
        try
        {
            if (id != dto.MenuItemId)
                return BadRequest(new { success = false, message = "El ID de la URL no coincide con el del body" });

            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });

            // Verificar que el item existe
            var existingItem = await _menuService.GetMenuItemByIdAsync(id);
            if (existingItem == null)
                return NotFound(new { success = false, message = $"No se encontró el item de menú con ID {id}" });

            await _menuService.UpdateMenuItemAsync(dto, userId);

            return Ok(new { success = true, message = "Item de menú actualizado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar el item de menú {MenuItemId}", id);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Eliminar un item de menú (soft delete)
    /// </summary>
    [HttpDelete("{id:int}")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> DeleteMenuItem(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });

            // Verificar que el item existe
            var existingItem = await _menuService.GetMenuItemByIdAsync(id);
            if (existingItem == null)
                return NotFound(new { success = false, message = $"No se encontró el item de menú con ID {id}" });

            await _menuService.DeleteMenuItemAsync(id, userId);

            return Ok(new { success = true, message = "Item de menú eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el item de menú {MenuItemId}", id);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Asignar roles a un item de menú
    /// </summary>
    [HttpPost("{id:int}/roles")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> AssignRolesToMenu(int id, [FromBody] int[] roleIds)
    {
        try
        {
            if (roleIds == null || roleIds.Length == 0)
                return BadRequest(new { success = false, message = "Debe proporcionar al menos un rol" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });

            // Verificar que el item existe
            var existingItem = await _menuService.GetMenuItemByIdAsync(id);
            if (existingItem == null)
                return NotFound(new { success = false, message = $"No se encontró el item de menú con ID {id}" });

            await _menuService.AssignRolesToMenuAsync(id, roleIds, userId);

            return Ok(new { success = true, message = "Roles asignados exitosamente al item de menú" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar roles al item de menú {MenuItemId}", id);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtener roles asignados a un item de menú
    /// </summary>
    [HttpGet("{id:int}/roles")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> GetRolesByMenuItem(int id)
    {
        try
        {
            var roles = await _menuService.GetRolesByMenuItemAsync(id);
            return Ok(new { success = true, data = roles });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener roles del item de menú {MenuItemId}", id);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtener menú por rol (para previsualización)
    /// </summary>
    [HttpGet("by-role/{roleId:int}")]
    public async Task<IActionResult> GetMenuByRole(int roleId)
    {
        try
        {
            var menu = await _menuService.GetMenuByRoleIdAsync(roleId);
            return Ok(new { success = true, data = menu });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener menú por rol {RoleId}", roleId);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Reordenar múltiples menús
    /// </summary>
    [HttpPost("reorder")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> ReorderMenus([FromBody] InfraMenu.ReorderMenusDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });

            await _menuService.ReorderMenusAsync(dto, userId);

            return Ok(new { success = true, message = "Menús reordenados exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reordenar menús");
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Mover un menú a otro padre y posición
    /// </summary>
    [HttpPost("move")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> MoveMenuItem([FromBody] InfraMenu.MoveMenuItemDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });

            await _menuService.MoveMenuItemAsync(dto, userId);

            return Ok(new { success = true, message = "Menú movido exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al mover menú {MenuItemId}", dto.MenuItemId);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Intercambiar posición de dos menús
    /// </summary>
    [HttpPost("swap")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> SwapMenuOrder([FromBody] InfraMenu.SwapMenuOrderDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });

            await _menuService.SwapMenuOrderAsync(dto, userId);

            return Ok(new { success = true, message = "Menús intercambiados exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al intercambiar menús {Id1} y {Id2}", dto.MenuItemId1, dto.MenuItemId2);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Reindexar hermanos (mismo padre) para que tengan índices consecutivos
    /// </summary>
    [HttpPost("reindex-siblings")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> ReindexSiblings([FromQuery] int? parentId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });

            await _menuService.ReindexSiblingsAsync(parentId, userId);

            return Ok(new { success = true, message = "Hermanos reindexados exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reindexar hermanos del padre {ParentId}", parentId);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }
}

/// <summary>
/// Controlador público para obtener el menú del usuario autenticado
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserMenuController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ILogger<UserMenuController> _logger;

    public UserMenuController(IMenuService menuService, ILogger<UserMenuController> logger)
    {
        _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtener el menú del usuario autenticado (principal endpoint para el frontend)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyMenu()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });

            var menu = await _menuService.GetMenuByUserIdAsync(userId);

            return Ok(menu); // Retorna directamente el array para que el frontend lo consuma fácilmente
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el menú del usuario {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }

    /// <summary>
    /// Verificar si el usuario tiene acceso a un menú específico
    /// </summary>
    [HttpGet("has-access/{menuKey}")]
    public async Task<IActionResult> HasAccess(string menuKey)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Usuario no autenticado" });

            var hasAccess = await _menuService.UserHasMenuAccessAsync(userId, menuKey);

            return Ok(new { success = true, hasAccess });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar acceso al menú {MenuKey}", menuKey);
            return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
        }
    }
}

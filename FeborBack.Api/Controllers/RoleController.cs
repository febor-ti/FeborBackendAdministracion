using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeborBack.Application.Services;
using FeborBack.Api.Authorization;

namespace FeborBack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // Solo requiere estar autenticado
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
    }

    [HttpGet]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> GetAllRoles()
    {
        try
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveRoles()
    {
        try
        {
            var roles = await _roleService.GetActiveRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    [HttpGet("{id:int}")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> GetRoleById(int id)
    {
        try
        {
            var role = await _roleService.GetRoleByIdAsync(id);

            if (role == null)
                return NotFound($"No se encontró el rol con ID {id}");

            return Ok(role);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    [HttpGet("name/{name}")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> GetRoleByName(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("El nombre del rol es requerido");

            var role = await _roleService.GetRoleByNameAsync(name);

            if (role == null)
                return NotFound($"No se encontró el rol con nombre '{name}'");

            return Ok(role);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtener usuarios que tienen asignado un rol específico
    /// </summary>
    [HttpGet("{id:int}/users")]
    [MenuAuthorize("manage", "admin")]
    public async Task<IActionResult> GetUsersByRole(int id)
    {
        try
        {
            var users = await _roleService.GetUsersByRoleIdAsync(id);

            return Ok(new
            {
                success = true,
                message = "Usuarios obtenidos exitosamente",
                data = users
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Obtener roles para selección en formularios (solo activos, formato simplificado)
    /// </summary>
    [HttpGet("for-selection")]
    public async Task<IActionResult> GetRolesForSelection()
    {
        try
        {
            var roles = await _roleService.GetActiveRolesAsync();
            var roleOptions = roles.Select(r => new
            {
                id = r.RoleId,
                name = r.RoleName,
                description = r.Description
            });

            return Ok(new
            {
                success = true,
                message = "Roles obtenidos exitosamente",
                data = roleOptions
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = $"Error interno del servidor: {ex.Message}"
            });
        }
    }
}
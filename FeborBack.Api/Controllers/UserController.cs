using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FeborBack.Application.DTOs.User;

// Alias para evitar conflictos
using UserServices = FeborBack.Application.Services.User;

namespace FeborBack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserServices.IUserManagementService _userManagementService;
    private readonly UserServices.IUserSupportService _userSupportService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        UserServices.IUserManagementService userManagementService,
        UserServices.IUserSupportService userSupportService,
        ILogger<UserController> logger)
    {
        _userManagementService = userManagementService;
        _userSupportService = userSupportService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Filtros inválidos", errors = ModelState });

            var result = await _userManagementService.GetUsersAsync(filter);
            return Ok(new { success = true, message = "Usuarios obtenidos exitosamente", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios");
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        try
        {
            var result = await _userManagementService.GetUserByIdAsync(id);
            if (result == null)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            return Ok(new { success = true, message = "Usuario obtenido exitosamente", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario {UserId}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("nameid")?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                return Unauthorized(new { success = false, message = "Token inválido" });

            var result = await _userManagementService.UpdateUserAsync(id, request, currentUserId);
            return Ok(new { success = true, message = "Usuario actualizado exitosamente", data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario {UserId}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("nameid")?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                return Unauthorized(new { success = false, message = "Token inválido" });

            if (currentUserId == id)
                return BadRequest(new { success = false, message = "No puedes eliminar tu propia cuenta" });

            var result = await _userManagementService.DeleteUserAsync(id, currentUserId);
            if (!result)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            return Ok(new { success = true, message = "Usuario eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar usuario {UserId}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("{id:int}/roles")]
    public async Task<IActionResult> AssignRoles(int id, [FromBody] List<int> roleIds)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("nameid")?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                return Unauthorized(new { success = false, message = "Token inválido" });

            var result = await _userManagementService.AssignRolesAsync(id, roleIds, currentUserId);
            if (!result)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            return Ok(new { success = true, message = "Roles asignados exitosamente" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar roles al usuario {UserId}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("{id:int}/block")]
    public async Task<IActionResult> BlockUser(int id, [FromBody] BlockUserDto request)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("nameid")?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                return Unauthorized(new { success = false, message = "Token inválido" });

            if (currentUserId == id)
                return BadRequest(new { success = false, message = "No puedes bloquear tu propia cuenta" });

            var result = await _userManagementService.BlockUserAsync(id, request.StatusReasonId, currentUserId);
            if (!result)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            return Ok(new { success = true, message = "Usuario bloqueado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al bloquear usuario {UserId}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("{id:int}/unblock")]
    public async Task<IActionResult> UnblockUser(int id)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("nameid")?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                return Unauthorized(new { success = false, message = "Token inválido" });

            var result = await _userManagementService.UnblockUserAsync(id, currentUserId);
            if (!result)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            return Ok(new { success = true, message = "Usuario desbloqueado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desbloquear usuario {UserId}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(int id)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("nameid")?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                return Unauthorized(new { success = false, message = "Token inválido" });

            var result = await _userManagementService.ResetUserPasswordAsync(id, currentUserId);
            if (!result)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            return Ok(new { success = true, message = "Contraseña reseteada exitosamente. Se ha generado una contraseña temporal." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al resetear contraseña del usuario {UserId}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatusList()
    {
        try
        {
            var result = await _userSupportService.GetStatusListAsync();
            return Ok(new { success = true, message = "Estados obtenidos exitosamente", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de estados");
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("status-reasons")]
    public async Task<IActionResult> GetStatusReasons()
    {
        try
        {
            var result = await _userSupportService.GetStatusReasonsAsync();
            return Ok(new { success = true, message = "Razones de estado obtenidas exitosamente", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener razones de estado");
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest(new { success = false, message = "Término de búsqueda requerido" });

            if (searchTerm.Length < 2)
                return BadRequest(new { success = false, message = "El término de búsqueda debe tener al menos 2 caracteres" });

            var result = await _userSupportService.SearchUsersAsync(searchTerm, limit);
            return Ok(new { success = true, message = "Búsqueda completada exitosamente", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuarios");
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }
}

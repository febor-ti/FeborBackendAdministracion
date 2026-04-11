using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FeborBack.Application.Services.Authorization;

namespace FeborBack.Api.Controllers;

/// <summary>
/// Controller temporal para debug de autorización
/// ELIMINAR EN PRODUCCIÓN
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DebugAuthController : ControllerBase
{
    private readonly IMenuAuthorizationService _authService;

    public DebugAuthController(IMenuAuthorizationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Ver información del usuario autenticado
    /// </summary>
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new
        {
            userId = userIdClaim,
            username,
            email,
            roles,
            allClaims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    /// <summary>
    /// Verificar si tiene acceso a un claim específico
    /// </summary>
    [HttpGet("check-claim")]
    public async Task<IActionResult> CheckClaim([FromQuery] string action, [FromQuery] string subject)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return BadRequest("No se pudo obtener el userId");
        }

        var hasAccess = await _authService.UserHasClaimAccessAsync(userId, action, subject);

        return Ok(new
        {
            userId,
            action,
            subject,
            hasAccess,
            message = hasAccess ? "✅ Tiene acceso" : "❌ NO tiene acceso"
        });
    }

    /// <summary>
    /// Ver todos los claims del usuario
    /// </summary>
    [HttpGet("my-claims")]
    public async Task<IActionResult> GetMyClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return BadRequest("No se pudo obtener el userId");
        }

        var claims = await _authService.GetUserClaimsAsync(userId);

        return Ok(new
        {
            userId,
            totalClaims = claims.Count(),
            claims
        });
    }

    /// <summary>
    /// Verificar si tiene acceso por menu_key
    /// </summary>
    [HttpGet("check-menu-key")]
    public async Task<IActionResult> CheckMenuKey([FromQuery] string menuKey)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return BadRequest("No se pudo obtener el userId");
        }

        var hasAccess = await _authService.UserHasMenuKeyAccessAsync(userId, menuKey);

        return Ok(new
        {
            userId,
            menuKey,
            hasAccess,
            message = hasAccess ? "✅ Tiene acceso" : "❌ NO tiene acceso"
        });
    }
}

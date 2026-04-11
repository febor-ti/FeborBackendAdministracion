using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using FeborBack.Application.Services.Authorization;

namespace FeborBack.Api.Authorization;

/// <summary>
/// Atributo de autorización basado en menu_key
/// Verifica si el usuario tiene acceso a un menú específico por su key
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class MenuKeyAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string MenuKey { get; }

    /// <summary>
    /// Constructor del atributo
    /// </summary>
    /// <param name="menuKey">Key del menú (ej: "users-create", "reports-view")</param>
    public MenuKeyAuthorizeAttribute(string menuKey)
    {
        MenuKey = menuKey ?? throw new ArgumentNullException(nameof(menuKey));
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Verificar que el usuario esté autenticado
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = "Usuario no autenticado"
            });
            return;
        }

        // Obtener el userId del claim
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = "No se pudo identificar al usuario"
            });
            return;
        }

        // Obtener el servicio de autorización
        var authService = context.HttpContext.RequestServices
            .GetService<IMenuAuthorizationService>();

        if (authService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Verificar si el usuario tiene acceso
        var hasAccess = await authService.UserHasMenuKeyAccessAsync(userId, MenuKey);

        if (!hasAccess)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}

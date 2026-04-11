using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using FeborBack.Application.Services.Authorization;
using Microsoft.Extensions.Logging;

namespace FeborBack.Api.Authorization;

/// <summary>
/// Atributo de autorización basado en menús
/// Verifica si el usuario tiene acceso al claim (action + subject) basado en sus roles y menús asignados
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class MenuAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string Action { get; }
    public string Subject { get; }

    /// <summary>
    /// Constructor del atributo
    /// </summary>
    /// <param name="action">Acción del claim (ej: "create", "read", "update", "delete")</param>
    /// <param name="subject">Sujeto del claim (ej: "users", "roles", "reports")</param>
    public MenuAuthorizeAttribute(string action, string subject)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetService<ILogger<MenuAuthorizeAttribute>>();

        // Verificar que el usuario esté autenticado
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            logger?.LogWarning("Authorization failed: User not authenticated");
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = "Usuario no autenticado"
            });
            return;
        }

        // .NET 9 JsonWebTokenHandler no remapea "nameid" → ClaimTypes.NameIdentifier.
        // Buscamos en ambas variantes y como último recurso leemos el JWT directamente.
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? context.HttpContext.User.FindFirst("nameid")?.Value;

        if (userIdClaim == null)
        {
            var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ") == true)
            {
                try
                {
                    var rawToken = authHeader.Substring("Bearer ".Length).Trim();
                    var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
                    userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
                }
                catch { }
            }
        }

        if (!int.TryParse(userIdClaim, out int userId))
        {
            logger?.LogWarning("Authorization failed: Could not parse userId from claim: {UserIdClaim}", userIdClaim);
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = "No se pudo identificar al usuario"
            });
            return;
        }

        logger?.LogInformation("Checking authorization for userId={UserId}, action={Action}, subject={Subject}", userId, Action, Subject);

        // Obtener el servicio de autorización
        var authService = context.HttpContext.RequestServices
            .GetService<IMenuAuthorizationService>();

        if (authService == null)
        {
            logger?.LogError("Authorization service not found in DI container");
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Verificar si el usuario tiene acceso
        var hasAccess = await authService.UserHasClaimAccessAsync(userId, Action, Subject);

        logger?.LogInformation("Authorization result for userId={UserId}, action={Action}, subject={Subject}: {HasAccess}", userId, Action, Subject, hasAccess);

        if (!hasAccess)
        {
            logger?.LogWarning("Access denied for userId={UserId}, action={Action}, subject={Subject}", userId, Action, Subject);
            context.Result = new ObjectResult(new
            {
                success = false,
                message = $"No tiene permisos para la acción '{Action}' en '{Subject}'",
                userId,
                requiredAction = Action,
                requiredSubject = Subject
            })
            {
                StatusCode = 403
            };
            return;
        }

        logger?.LogInformation("Access granted for userId={UserId}, action={Action}, subject={Subject}", userId, Action, Subject);
    }
}

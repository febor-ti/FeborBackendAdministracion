namespace FeborBack.Application.Services.Authorization;

/// <summary>
/// Servicio de autorización basado en menús
/// </summary>
public interface IMenuAuthorizationService
{
    /// <summary>
    /// Verifica si un usuario tiene acceso a un claim específico (action + subject)
    /// </summary>
    Task<bool> UserHasClaimAccessAsync(int userId, string action, string subject);

    /// <summary>
    /// Verifica si un usuario tiene acceso a un menú por su menu_key
    /// </summary>
    Task<bool> UserHasMenuKeyAccessAsync(int userId, string menuKey);

    /// <summary>
    /// Obtiene todos los claims que tiene un usuario
    /// </summary>
    Task<IEnumerable<UserClaimDto>> GetUserClaimsAsync(int userId);
}

/// <summary>
/// DTO para representar un claim de usuario
/// </summary>
public class UserClaimDto
{
    public string ClaimAction { get; set; } = string.Empty;
    public string ClaimSubject { get; set; } = string.Empty;
    public string? MenuKey { get; set; }
    public string? MenuTitle { get; set; }
}

namespace FeborBack.Infrastructure.Repositories.Authorization;

/// <summary>
/// Repositorio para verificación de autorización basada en menús
/// </summary>
public interface IMenuAuthorizationRepository
{
    Task<bool> UserHasClaimAccessAsync(int userId, string action, string subject);
    Task<bool> UserHasMenuKeyAccessAsync(int userId, string menuKey);
    Task<IEnumerable<UserClaimDto>> GetUserClaimsAsync(int userId);
}

public class UserClaimDto
{
    public string claim_action { get; set; } = string.Empty;
    public string claim_subject { get; set; } = string.Empty;
    public string? menu_key { get; set; }
    public string? menu_title { get; set; }
}

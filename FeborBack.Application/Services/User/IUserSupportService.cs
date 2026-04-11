using FeborBack.Application.DTOs.User;

namespace FeborBack.Application.Services.User;

/// <summary>
/// Interfaz para servicios de soporte de usuarios
/// </summary>
public interface IUserSupportService
{
    Task<List<StatusDto>> GetStatusListAsync();
    Task<List<StatusReasonDto>> GetStatusReasonsAsync();
    Task<List<UserSearchResultDto>> SearchUsersAsync(string searchTerm, int limit = 10);
}
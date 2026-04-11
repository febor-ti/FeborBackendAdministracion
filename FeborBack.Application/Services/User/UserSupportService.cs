using FeborBack.Application.DTOs.User;
using FeborBack.Infrastructure.Repositories;

namespace FeborBack.Application.Services.User;

/// <summary>
/// Servicio de soporte para usuarios
/// </summary>
public class UserSupportService : IUserSupportService
{
    private readonly IUserRepository _userRepository;

    public UserSupportService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<StatusDto>> GetStatusListAsync()
    {
        try
        {
            var infraStatusList = await _userRepository.GetStatusListAsync();

            // Mapear de Infrastructure DTO a Application DTO
            return infraStatusList.Select(s => new StatusDto
            {
                StatusId = s.StatusId,
                StatusName = s.StatusName
            }).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener lista de estados: {ex.Message}", ex);
        }
    }

    public async Task<List<StatusReasonDto>> GetStatusReasonsAsync()
    {
        try
        {
            var infraStatusReasons = await _userRepository.GetStatusReasonsAsync();

            // Mapear de Infrastructure DTO a Application DTO
            return infraStatusReasons.Select(sr => new StatusReasonDto
            {
                StatusReasonId = sr.StatusReasonId,
                ReasonName = sr.ReasonName
            }).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener razones de estado: {ex.Message}", ex);
        }
    }

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(string searchTerm, int limit = 10)
    {
        try
        {
            var infraResults = await _userRepository.SearchUsersAsync(searchTerm, limit);

            // Mapear de Infrastructure DTO a Application DTO
            return infraResults.Select(r => new UserSearchResultDto
            {
                UserId = r.UserId,
                Username = r.Username,
                Email = r.Email,
                FullName = r.FullName,
                StatusName = r.StatusName
            }).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al buscar usuarios: {ex.Message}", ex);
        }
    }
}
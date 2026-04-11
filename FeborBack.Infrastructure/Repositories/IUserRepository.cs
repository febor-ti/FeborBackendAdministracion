using FeborBack.Infrastructure.DTOs;
using FeborBack.Domain.Entities;

namespace FeborBack.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<(List<LoginUser> users, int totalCount)> GetUsersPagedAsync(UserFilterDto filter);
    Task<LoginUser?> GetUserByIdWithDetailsAsync(int userId);
    Task<List<UserRoleDto>> GetUserRolesDetailAsync(int userId);
    Task<List<StatusDto>> GetStatusListAsync();
    Task<List<StatusReasonDto>> GetStatusReasonsAsync();
    Task<List<UserSearchResultDto>> SearchUsersAsync(string searchTerm, int limit = 10);
}